using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

// Opt-in: requiere broker real. Correr con:
//   docker compose -f infra/docker-compose.yml up -d rabbitmq
//   RABBITMQ_TEST_HOST=localhost dotnet test tests/Umbral.OperacionesSesion.IntegrationTests/... --filter RabbitMqRoundTripTests
// Sin RABBITMQ_TEST_HOST el test retorna sin assertar (skip suave, sin dependencia de paquetes extra).
public class RabbitMqRoundTripTests
{
    [Fact]
    public async Task Publicar_llega_a_una_cola_bindeada_con_el_envelope_esperado()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host)) return; // opt-in

        var options = new RabbitMqOptions { Enabled = true, Host = host };
        using var canal = new RabbitMqPublishChannel(options);
        var publisher = new RabbitMqSesionEventsPublisher(canal, TimeProvider.System,
            NullLogger<RabbitMqSesionEventsPublisher>.Instance);

        var factory = new ConnectionFactory { HostName = host, Port = options.Port, UserName = options.User, Password = options.Password };
        using var connection = factory.CreateConnection("umbral-integration-test");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = channel.QueueDeclare($"test.roundtrip.{Guid.NewGuid():N}", durable: false, exclusive: true, autoDelete: true).QueueName;
        channel.QueueBind(queue, options.Exchange, "operaciones-sesion.#");

        var partidaId = Guid.NewGuid();
        await publisher.PublicarPartidaIniciadaAsync(
            new PartidaIniciadaEvent(partidaId, Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), 1), default);

        BasicGetResult? result = null;
        for (var i = 0; i < 50 && result is null; i++) // hasta ~5 s
        {
            result = channel.BasicGet(queue, autoAck: true);
            if (result is null) await Task.Delay(100);
        }

        Assert.NotNull(result);
        Assert.Equal("operaciones-sesion.partida-iniciada.v1", result!.RoutingKey);
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(result.Body.ToArray()));
        Assert.Equal("PartidaIniciada", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(partidaId, doc.RootElement.GetProperty("payload").GetProperty("partidaId").GetGuid());
    }
}
