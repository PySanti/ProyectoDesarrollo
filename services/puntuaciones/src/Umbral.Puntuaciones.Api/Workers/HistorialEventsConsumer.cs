using MediatR;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor del historial (SP-4d): segunda cola con binding # al exchange existente; cada evento
// se traduce al comando genérico y se despacha con scope propio (sin pipeline de difusión — el
// historial no difunde). Best-effort (ADR-0012): ack-siempre, sin poison-loop; el historial es
// reconstruible reprocesando eventos. Mismo esqueleto que OperacionesSesionEventsConsumer.
public sealed class HistorialEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _conexion;
    private readonly RabbitMqHistorialOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HistorialEventsConsumer> _logger;

    public HistorialEventsConsumer(
        RabbitMqConsumerOptions conexion,
        RabbitMqHistorialOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<HistorialEventsConsumer> logger)
    {
        _conexion = conexion;
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_conexion.Enabled || string.IsNullOrWhiteSpace(_conexion.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de historial no arranca.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _conexion.Host,
                    Port = _conexion.Port,
                    UserName = _conexion.User,
                    Password = _conexion.Password,
                    DispatchConsumersAsync = true
                };
                using var connection = factory.CreateConnection("umbral-puntuaciones-historial");
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_conexion.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(_options.Queue, _conexion.Exchange, _options.Binding);

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
                _logger.LogWarning(ex, "Conexión RabbitMQ del historial caída; reintento en 30 s.");
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

        var command = HistorialEventMapper.Map(envelope!);
        if (command is null)
        {
            _logger.LogWarning(
                "Evento {EventType} {EventId} sin registro de historial; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(command, ct);
        }
        catch (DbUpdateException)
        {
            // Carrera del check-then-insert entre consumidores: el índice único de EventId ya
            // registró la fila — el duplicado ES el resultado correcto (design SP-4d §3).
            _logger.LogInformation(
                "Evento de historial {EventType} {EventId} ya registrado por otro consumidor.",
                envelope!.EventType, envelope.EventId);
        }
        catch (Exception ex)
        {
            // Best-effort (ADR-0012): el historial es reconstruible; sin requeue para evitar poison-loop.
            _logger.LogError(ex, "Fallo registrando historial {EventType} {EventId}; se descarta (ack).",
                envelope!.EventType, envelope.EventId);
        }
        finally
        {
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }
}
