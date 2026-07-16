using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.Api.Workers;

// Segundo consumidor RabbitMQ de Identity (7f, RNF-23): Identity se autoconsume su propio
// evento CredencialTemporalEmitida (exchange umbral.identity) para disparar el correo SMTP de
// bienvenida sin bloquear la creación del usuario. Calcado de OperacionesInscripcionesConsumer:
// conexión con reintento a 30 s, exchange/cola/bindings durables, ack-siempre best-effort
// (ADR-0012) — un fallo de SMTP se loguea y se descarta, nunca se reintenta (sin poison-loop).
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
            eventType != "CredencialTemporalEmitida")
        {
            _logger.LogWarning("Envelope malformado o inesperado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IUserWelcomeEmailSender>();
            await EnviarBestEffortAsync(sender, payload, _logger, ea.RoutingKey, ct);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): fallo resolviendo el scope/sender. Nunca se reintenta.
            _logger.LogError(ex, "Fallo preparando el envío del correo de credenciales (rk {RoutingKey}); se descarta (ack).",
                ea.RoutingKey);
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

    private static string LeerString(JsonElement payload, string prop) =>
        payload.TryGetProperty(prop, out var valor) ? valor.GetString() ?? string.Empty : string.Empty;
}
