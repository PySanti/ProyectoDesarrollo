using RabbitMQ.Client;

namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

public sealed class RabbitMqPublishChannel : IRabbitMqPublishChannel, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublishChannel(RabbitMqOptions options) => _options = options;

    public void Publish(string routingKey, byte[] body)
    {
        lock (_lock)
        {
            EnsureChannel();
            var props = _channel!.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent
            _channel.BasicPublish(_options.Exchange, routingKey, basicProperties: props, body: body);
        }
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;
        _channel?.Dispose();
        if (_connection is not { IsOpen: true })
        {
            _connection?.Dispose();
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password
            };
            _connection = factory.CreateConnection("umbral-identity-publisher");
        }
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
