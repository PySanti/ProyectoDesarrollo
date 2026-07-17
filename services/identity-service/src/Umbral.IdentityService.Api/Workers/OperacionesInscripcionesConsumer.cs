using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.Api.Workers;

// Primer consumidor RabbitMQ de Identity (Task D3): mantiene la proyección
// participaciones_activas_equipo que respalda el guard BR-E10 (no eliminar un equipo con
// participación activa en una partida). Calcado del consumidor de Puntuaciones
// (OperacionesSesionEventsConsumer): conexión con reintento a 30 s, exchange/cola/bindings
// durables, ack-siempre best-effort (ADR-0012) — sin poison-loop, la proyección es reconstruible.
public sealed class OperacionesInscripcionesConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperacionesInscripcionesConsumer> _logger;

    public OperacionesInscripcionesConsumer(
        RabbitMqConsumerOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<OperacionesInscripcionesConsumer> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    // los bindgins viven en Infrastructure\Services\Messaging\RabbitMqConsumerOptions.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de participaciones no arranca.");
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
                using var connection = factory.CreateConnection("umbral-identity-participaciones-consumer");
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
        if (!OperacionesEnvelopeReader.TryRead(ea.Body.Span, out var eventType, out var payload))
        {
            _logger.LogWarning("Envelope malformado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            // Application\Services\ParticipacionProjectionUpdater.cs
            var updater = scope.ServiceProvider.GetRequiredService<IParticipacionProjectionUpdater>();
            await updater.AplicarAsync(eventType, payload, ct);
            _logger.LogInformation(
                "Evento proyectado {EventType} (rk {RoutingKey}).", eventType, ea.RoutingKey);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): incluye DbUpdateException por conflicto de escritura
            // concurrente en el upsert (check-then-add). La proyección es reconstruible desde
            // los eventos de Operaciones de Sesión; nunca se reintenta/requeue para evitar
            // poison-loop.
            _logger.LogError(ex, "Fallo proyectando {EventType} (rk {RoutingKey}); se descarta (ack).",
                eventType, ea.RoutingKey);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }
}
