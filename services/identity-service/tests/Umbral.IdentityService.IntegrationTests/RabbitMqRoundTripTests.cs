using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.IntegrationTests;

// Opt-in: requiere broker real. Correr con:
//   docker compose -f infra/docker-compose.yml up -d rabbitmq
//   RABBITMQ_TEST_HOST=localhost dotnet test tests/Umbral.IdentityService.IntegrationTests/... --filter RabbitMqRoundTripTests
// Sin RABBITMQ_TEST_HOST el test retorna sin assertar (skip suave, patrón SP-3i).
public class RabbitMqRoundTripTests
{
    [Fact]
    public async Task PermisosRolActualizados_llega_al_broker_real()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return; // opt-in: sin broker configurado el test es un no-op (skip suave, patrón SP-3i)
        }

        var options = new RabbitMqOptions { Enabled = true, Host = host };
        using var canal = new RabbitMqPublishChannel(options);
        var publisher = new RabbitMqIdentityEventsPublisher(canal, TimeProvider.System,
            NullLogger<RabbitMqIdentityEventsPublisher>.Instance);

        var factory = new ConnectionFactory { HostName = host, Port = options.Port, UserName = options.User, Password = options.Password };
        using var connection = factory.CreateConnection("umbral-identity-roundtrip-test");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true).QueueName;
        channel.QueueBind(queue, options.Exchange, "identity.#");

        await publisher.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, DateTime.UtcNow),
            CancellationToken.None);

        BasicGetResult? result = null;
        for (var i = 0; i < 50 && result is null; i++)
        {
            result = channel.BasicGet(queue, autoAck: true);
            if (result is null) await Task.Delay(100);
        }

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(result!.Body.ToArray()));
        Assert.Equal("PermisosRolActualizados", doc.RootElement.GetProperty("eventType").GetString());
    }
}
