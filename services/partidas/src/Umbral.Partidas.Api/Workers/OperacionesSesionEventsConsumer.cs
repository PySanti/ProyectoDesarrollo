using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Partidas.Api.Workers;

// Consumidor de los eventos de ciclo de vida de la partida (fix 4): proyecta el estado de runtime
// que reporta Operaciones de Sesión sobre la entidad Partida, para que el listado del operador deje
// de mostrar todo como "Sin publicar". Calcado del consumidor de Identity/Puntuaciones: conexión
// con reintento a 30 s, exchange/cola/bindings durables, ack-siempre best-effort (ADR-0012) — la
// proyección es reconstruible, así que nunca se hace requeue (evita poison-loop).
public sealed class OperacionesSesionEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperacionesSesionEventsConsumer> _logger;

    public OperacionesSesionEventsConsumer(
        RabbitMqConsumerOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<OperacionesSesionEventsConsumer> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de estado de partidas no arranca.");
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
                using var connection = factory.CreateConnection("umbral-partidas-estado-consumer");
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
        if (!EnvelopeReader.TryRead(ea.Body.Span, out var envelope))
        {
            _logger.LogWarning("Envelope malformado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        var command = EstadoPartidaEventMapper.Map(envelope!);
        if (command is null)
        {
            _logger.LogWarning(
                "Evento {EventType} {EventId} sin proyección de estado; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(command, ct);
            _logger.LogInformation(
                "Estado proyectado {Estado} para partida {PartidaId} (evento {EventType}).",
                command.Estado, command.PartidaId, envelope!.EventType);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): la proyección es reconstruible; sin requeue para evitar poison-loop.
            _logger.LogError(ex, "Fallo proyectando {EventType} {EventId}; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }
}
