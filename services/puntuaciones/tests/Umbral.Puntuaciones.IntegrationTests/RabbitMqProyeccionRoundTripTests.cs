using System.Net;
using System.Text;
using System.Text.Json;
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
        // PuntuacionesWebFactory es sealed y WithWebHostBuilder() de WebApplicationFactory<T> devuelve el
        // tipo base (perdiendo CreateClientAutenticado); la config de RabbitMq para este test puntual se
        // aplica por variables de entorno, que IConfiguration lee igual que appsettings.
        Environment.SetEnvironmentVariable("RabbitMq__Enabled", "true");
        Environment.SetEnvironmentVariable("RabbitMq__Host", host);
        Environment.SetEnvironmentVariable("RabbitMq__Queue", testQueue);
        try
        {
            await using var factory = new PuntuacionesWebFactory();
            var client = factory.CreateClientAutenticado(); // arranca el host y el consumidor

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
        finally
        {
            Environment.SetEnvironmentVariable("RabbitMq__Enabled", null);
            Environment.SetEnvironmentVariable("RabbitMq__Host", null);
            Environment.SetEnvironmentVariable("RabbitMq__Queue", null);
        }
    }
}
