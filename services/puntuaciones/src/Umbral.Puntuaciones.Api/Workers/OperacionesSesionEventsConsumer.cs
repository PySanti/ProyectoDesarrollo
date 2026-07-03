using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor de humo (SP-3i): declara la infraestructura para SP-4 y loguea cada evento.
// Sin DB, sin proyecciones. SP-4 lo reemplaza por el consumidor real.
public sealed class OperacionesSesionEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly ILogger<OperacionesSesionEventsConsumer> _logger;

    public OperacionesSesionEventsConsumer(RabbitMqConsumerOptions options, ILogger<OperacionesSesionEventsConsumer> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de eventos no arranca.");
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
                    DispatchConsumersAsync = false
                };
                using var connection = factory.CreateConnection("umbral-puntuaciones-consumer");
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(_options.Queue, _options.Exchange, _options.Binding);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (_, ea) =>
                {
                    if (EnvelopeReader.TryRead(ea.Body.Span, out var envelope))
                    {
                        _logger.LogInformation(
                            "Evento recibido {EventType} {EventId} v{Version} (rk {RoutingKey}, occurredAt {OccurredAt:O})",
                            envelope!.EventType, envelope.EventId, envelope.Version, ea.RoutingKey, envelope.OccurredAt);
                    }
                    else
                    {
                        _logger.LogWarning("Envelope malformado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
                    }
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                };
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
}
