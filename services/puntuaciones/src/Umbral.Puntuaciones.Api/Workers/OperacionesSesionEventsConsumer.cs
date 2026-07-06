using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor real de proyecciones (SP-4a): mapea cada evento a su comando MediatR y lo despacha
// con un scope por mensaje. Best-effort (ADR-0012): ack-siempre, sin poison-loop; la proyección
// es reconstruible. Reemplaza al consumidor de humo de SP-3i.
public sealed class OperacionesSesionEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly ProyeccionPipeline _pipeline;
    private readonly ILogger<OperacionesSesionEventsConsumer> _logger;

    public OperacionesSesionEventsConsumer(
        RabbitMqConsumerOptions options,
        ProyeccionPipeline pipeline,
        ILogger<OperacionesSesionEventsConsumer> logger)
    {
        _options = options;
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de proyecciones no arranca.");
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
                using var connection = factory.CreateConnection("umbral-puntuaciones-consumer");
                EliminarColaDeHumoLegacy(connection);

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

        var command = ProyeccionEventMapper.Map(envelope!);
        if (command is null)
        {
            _logger.LogWarning(
                "Evento {EventType} {EventId} sin proyección en SP-4a; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            await _pipeline.EjecutarAsync(command, ct);
            _logger.LogInformation(
                "Evento proyectado {EventType} {EventId} (rk {RoutingKey}).",
                envelope!.EventType, envelope.EventId, ea.RoutingKey);
        }
        catch (DbUpdateException)
        {
            // Conflicto de escritura concurrente (SP-4b): xmin en UPDATE o clave única en INSERT
            // cuando otro consumidor pisó/creó la misma fila. Un único reintento con scope fresco
            // relee el estado actual; el dedup transaccional garantiza que el intento fallido no
            // dejó rastro.
            try
            {
                await _pipeline.EjecutarAsync(command, ct);
                _logger.LogInformation(
                    "Evento proyectado tras reintento por conflicto de escritura {EventType} {EventId}.",
                    envelope!.EventType, envelope.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Fallo persistente tras reintento proyectando {EventType} {EventId}; se descarta (ack).",
                    envelope!.EventType, envelope.EventId);
            }
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

    private void EliminarColaDeHumoLegacy(IConnection connection)
    {
        // Canal desechable propio: si la operación falla, no tumba el canal de consumo.
        try
        {
            using var channel = connection.CreateModel();
            channel.QueueDelete(RabbitMqConsumerOptions.ColaDeHumoLegacy, ifUnused: false, ifEmpty: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar la cola de humo legacy {Queue}.", RabbitMqConsumerOptions.ColaDeHumoLegacy);
        }
    }
}
