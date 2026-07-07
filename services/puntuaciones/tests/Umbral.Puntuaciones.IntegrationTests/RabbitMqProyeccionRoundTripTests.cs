using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using RabbitMQ.Client;

namespace Umbral.Puntuaciones.IntegrationTests;

// Round-trip real broker → consumidor → proyección → HTTP. Opt-in:
//   docker compose -f infra/docker-compose.yml up -d rabbitmq
//   RABBITMQ_TEST_HOST=localhost dotnet test tests/Umbral.Puntuaciones.IntegrationTests/... --filter RabbitMqProyeccionRoundTripTests
// Sin RABBITMQ_TEST_HOST el test retorna sin assertar (skip suave, sin dependencia de paquetes extra).
public class RabbitMqProyeccionRoundTripTests
{
    private const string Exchange = "umbral.operaciones-sesion";

    private static string EnvelopeJson(string eventType, object payload) => JsonSerializer.Serialize(new
    {
        eventId = Guid.NewGuid(),
        eventType,
        version = 1,
        occurredAt = DateTime.UtcNow,
        payload
    }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task Evento_publicado_al_broker_termina_en_el_ranking()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return; // skip suave: sin broker configurado no se asserta nada.
        }

        var testQueue = $"puntuaciones.proyecciones.it-{Guid.NewGuid():N}";
        // WithWebHostBuilder() de WebApplicationFactory<T> devuelve el tipo base (perdiendo
        // CreateClientAutenticado), pero permite configurar RabbitMq por instancia en vez de variables de
        // entorno de proceso, evitando que otra clase de test bajo xUnit paralelo herede este consumidor.
        await using var factory = new PuntuacionesWebFactory().WithWebHostBuilder(b =>
        {
            b.UseSetting("RabbitMq:Enabled", "true");
            b.UseSetting("RabbitMq:Host", host);
            b.UseSetting("RabbitMq:Queue", testQueue);
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", Guid.NewGuid().ToString());

        var connectionFactory = new ConnectionFactory { HostName = host };
        using var connection = connectionFactory.CreateConnection("umbral-puntuaciones-it");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // Esperar a que el consumidor haya declarado su cola (arranque asíncrono del BackgroundService).
        var declarada = false;
        for (var i = 0; i < 50 && !declarada; i++)
        {
            try
            {
                using var probe = connection.CreateModel();
                probe.QueueDeclarePassive(testQueue);
                declarada = true;
            }
            catch (Exception)
            {
                await Task.Delay(200);
            }
        }
        Assert.True(declarada, "El consumidor no declaró su cola a tiempo.");

        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        void Publicar(string routingKey, string json)
            => channel.BasicPublish(Exchange, routingKey, basicProperties: null, body: Encoding.UTF8.GetBytes(json));

        Publicar("operaciones-sesion.juego-activado.v1", EnvelopeJson("JuegoActivado",
            new { partidaId, sesionPartidaId = sesionId, juegoId, orden = 1, tipoJuego = "Trivia" }));
        Publicar("operaciones-sesion.puntaje-trivia-incrementado.v1", EnvelopeJson("PuntajeTriviaIncrementado",
            new { partidaId, sesionPartidaId = sesionId, juegoId, preguntaId = Guid.NewGuid(), participanteId, puntaje = 10, tiempoRespuestaMs = 1234, equipoId = (Guid?)null }));

        var proyectado = false;
        for (var i = 0; i < 50 && !proyectado; i++)
        {
            var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                proyectado = json.RootElement.GetProperty("entradas").GetArrayLength() == 1;
            }
            if (!proyectado)
            {
                await Task.Delay(200);
            }
        }

        channel.QueueDelete(testQueue, ifUnused: false, ifEmpty: false);
        Assert.True(proyectado, "El evento publicado al broker no llegó al ranking en 10 s.");
    }
}
