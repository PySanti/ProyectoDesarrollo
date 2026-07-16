using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.Api.Workers;

// Segundo consumidor RabbitMQ de Identity (7f, RNF-23): la cola de correos de Identity. Identity
// se autoconsume sus propios eventos (exchange umbral.identity) para enviar el correo SMTP sin
// bloquear la operación de dominio que lo origina — CredencialTemporalEmitida (bienvenida, no
// bloquea la creación del usuario) y EquipoEliminado (aviso a los integrantes, no bloquea la
// eliminación del equipo). Calcado de OperacionesInscripcionesConsumer: conexión con reintento a
// 30 s, exchange/cola/bindings durables, ack-siempre best-effort (ADR-0012) — un fallo de SMTP se
// loguea y se descarta, nunca se reintenta (sin poison-loop).
public sealed class CredencialesTemporalesConsumer : BackgroundService
{
    private readonly RabbitMqCredencialesConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CredencialesTemporalesConsumer> _logger;

    public CredencialesTemporalesConsumer(
        RabbitMqCredencialesConsumerOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<CredencialesTemporalesConsumer> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    // los bindings viven en Infrastructure\Services\Messaging\RabbitMqCredencialesConsumerOptions.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de correo de credenciales no arranca.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.User,
                    Password = _options.Password,
                    DispatchConsumersAsync = true
                };
                using var connection = factory.CreateConnection("umbral-identity-credenciales-consumer");
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                foreach (var binding in _options.Bindings)
                {
                    channel.QueueBind(_options.Queue, _options.Exchange, binding);
                }

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += (_, ea) => ProcesarMensajeAsync(channel, ea, stoppingToken);
                channel.BasicConsume(_options.Queue, autoAck: false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Conexión RabbitMQ caída; reintento en 30 s.");
                try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task ProcesarMensajeAsync(IModel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (!OperacionesEnvelopeReader.TryRead(ea.Body.Span, out var eventType, out var payload) ||
            eventType is not ("CredencialTemporalEmitida" or "EquipoEliminado" or "LiderazgoEquipoModificado"))
        {
            _logger.LogWarning("Envelope malformado o inesperado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            switch (eventType)
            {
                case "CredencialTemporalEmitida":
                    await EnviarBestEffortAsync(
                        scope.ServiceProvider.GetRequiredService<IUserWelcomeEmailSender>(),
                        payload, _logger, ea.RoutingKey, ct);
                    break;
                case "EquipoEliminado":
                    await NotificarEquipoEliminadoBestEffortAsync(
                        scope.ServiceProvider.GetRequiredService<ITeamLifecycleNotifier>(),
                        payload, _logger, ea.RoutingKey, ct);
                    break;
                default:
                    await NotificarLiderazgoModificadoBestEffortAsync(
                        scope.ServiceProvider.GetRequiredService<ITeamLifecycleNotifier>(),
                        payload, _logger, ea.RoutingKey, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): fallo resolviendo el scope/sender. Nunca se reintenta.
            _logger.LogError(ex, "Fallo preparando el envío del correo de {EventType} (rk {RoutingKey}); se descarta (ack).",
                eventType, ea.RoutingKey);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    /// <summary>
    /// Deserializa el payload camelCase del evento (<c>{ nombre, correo, rol, passwordTemporal }</c>)
    /// a <see cref="UserWelcomeEmailMessage"/>. Campos ausentes se leen como cadena vacía — nunca
    /// lanza, para no interrumpir el best-effort del consumidor.
    /// </summary>
    public static UserWelcomeEmailMessage MapPayload(JsonElement payload) => new(
        LeerString(payload, "nombre"),
        LeerString(payload, "correo"),
        LeerString(payload, "rol"),
        LeerString(payload, "passwordTemporal"));

    /// <summary>
    /// Mapea el payload y envía el correo vía <paramref name="sender"/>. Best-effort estricto:
    /// un fallo de SMTP (o cualquier otra excepción) se loguea y NUNCA se relanza, para que el
    /// consumidor siempre haga ack (sin poison-loop, ADR-0012).
    /// </summary>
    public static async Task EnviarBestEffortAsync(
        IUserWelcomeEmailSender sender,
        JsonElement payload,
        ILogger logger,
        string routingKey,
        CancellationToken ct)
    {
        var message = MapPayload(payload);
        try
        {
            await sender.SendWelcomeEmailAsync(message, ct);
            logger.LogInformation("Correo de credenciales enviado a {Correo} (rk {RoutingKey}).", message.Email, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo enviando el correo de credenciales a {Correo} (rk {RoutingKey}); se descarta (ack).",
                message.Email, routingKey);
        }
    }

    /// <summary>
    /// Notifica a los integrantes de un equipo eliminado a partir del payload camelCase del evento
    /// (<c>{ equipoId, nombreEquipo, origen, miembros: [guid...], occurredOnUtc }</c>). Los
    /// <c>miembros</c> vienen en espacio KeycloakId, que es lo que espera el notificador. Best-effort
    /// estricto: un payload roto o un fallo de SMTP se loguea y NUNCA se relanza, para que el
    /// consumidor siempre haga ack (sin poison-loop, ADR-0012).
    /// </summary>
    public static async Task NotificarEquipoEliminadoBestEffortAsync(
        ITeamLifecycleNotifier notifier,
        JsonElement payload,
        ILogger logger,
        string routingKey,
        CancellationToken ct)
    {
        var nombreEquipo = LeerString(payload, "nombreEquipo");
        var miembros = LeerGuids(payload, "miembros");
        if (miembros.Count == 0)
        {
            logger.LogInformation("Equipo {NombreEquipo} eliminado sin integrantes que notificar (rk {RoutingKey}).",
                nombreEquipo, routingKey);
            return;
        }

        try
        {
            var resultado = await notifier.NotificarEquipoEliminadoAsync(nombreEquipo, miembros, ct);
            logger.LogInformation(
                "Equipo {NombreEquipo} eliminado: {Notificados} de {Total} integrante(s) notificados (rk {RoutingKey}).",
                nombreEquipo, resultado.Notificados, resultado.Total, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo notificando la eliminación del equipo {NombreEquipo} (rk {RoutingKey}); se descarta (ack).",
                nombreEquipo, routingKey);
        }
    }

    /// <summary>
    /// Notifica al líder anterior y al nuevo a partir del payload camelCase del evento
    /// (<c>{ equipoId, liderAnteriorUserId, nuevoLiderUserId, origen, occurredOnUtc }</c>). Ambos
    /// ids vienen en espacio KeycloakId, que es lo que espera el notificador. Best-effort estricto:
    /// un payload roto o un fallo de SMTP se loguea y NUNCA se relanza (ack siempre, ADR-0012).
    /// </summary>
    public static async Task NotificarLiderazgoModificadoBestEffortAsync(
        ITeamLifecycleNotifier notifier,
        JsonElement payload,
        ILogger logger,
        string routingKey,
        CancellationToken ct)
    {
        var liderAnterior = LeerGuid(payload, "liderAnteriorUserId");
        var nuevoLider = LeerGuid(payload, "nuevoLiderUserId");
        if (liderAnterior == Guid.Empty || nuevoLider == Guid.Empty)
        {
            logger.LogWarning("Payload de liderazgo sin los dos líderes (rk {RoutingKey}); se descarta (ack).", routingKey);
            return;
        }

        try
        {
            await notifier.NotificarLiderazgoModificadoAsync(liderAnterior, nuevoLider, ct);
            logger.LogInformation("Liderazgo notificado: {LiderAnterior} → {NuevoLider} (rk {RoutingKey}).",
                liderAnterior, nuevoLider, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo notificando el cambio de liderazgo (rk {RoutingKey}); se descarta (ack).", routingKey);
        }
    }

    private static string LeerString(JsonElement payload, string prop) =>
        payload.TryGetProperty(prop, out var valor) ? valor.GetString() ?? string.Empty : string.Empty;

    private static Guid LeerGuid(JsonElement payload, string prop) =>
        payload.TryGetProperty(prop, out var valor) && valor.TryGetGuid(out var guid) ? guid : Guid.Empty;

    private static IReadOnlyList<Guid> LeerGuids(JsonElement payload, string prop)
    {
        if (!payload.TryGetProperty(prop, out var valores) || valores.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<Guid>();
        }

        var guids = new List<Guid>();
        foreach (var valor in valores.EnumerateArray())
        {
            if (valor.TryGetGuid(out var guid))
            {
                guids.Add(guid);
            }
        }

        return guids;
    }
}
