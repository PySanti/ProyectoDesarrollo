using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

// Forma de la entrada del historial: juegoOrden/tipoJuego unidos desde JuegoProyectado.
// Vive aqui y no en ContractTests porque el arnes de contrato no siembra datos (solo cubre
// auth y errores); el sembrado por comandos MediatR es el patron de este proyecto.
public class HistorialJuegoLabelE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = DateTime.UtcNow;

    public HistorialJuegoLabelE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    private static JsonElement EntradaDe(JsonDocument doc, string tipoEvento)
        => doc.RootElement.GetProperty("entradas").EnumerateArray()
            .Single(e => e.GetProperty("tipoEvento").GetString() == tipoEvento);

    [Fact]
    public async Task Entrada_de_juego_expone_juegoOrden_y_tipoJuego_y_la_de_partida_los_deja_null()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(
            Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 2, TipoJuego.Trivia));
        await Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), "RespuestaTriviaValidada", Ahora, partidaId, juegoId, null, null, "{}"));
        await Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), "PartidaIniciada", Ahora, partidaId, null, null, null, "{}"));

        var client = _factory.CreateClientConRoles("Operador");
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var deJuego = EntradaDe(doc, "RespuestaTriviaValidada");
        Assert.Equal(2, deJuego.GetProperty("juegoOrden").GetInt32());
        Assert.Equal("Trivia", deJuego.GetProperty("tipoJuego").GetString());

        // Un evento de partida no tiene juego: el cliente lo pinta como "—".
        var dePartida = EntradaDe(doc, "PartidaIniciada");
        Assert.Equal(JsonValueKind.Null, dePartida.GetProperty("juegoOrden").ValueKind);
        Assert.Equal(JsonValueKind.Null, dePartida.GetProperty("tipoJuego").ValueKind);
    }

    [Fact]
    public async Task Evento_con_juegoId_sin_proyeccion_deja_juegoOrden_null_pero_conserva_juegoId()
    {
        // El cliente distingue este caso del anterior y cae al GUID corto, no a "—".
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoHuerfano = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), "JuegoActivado", Ahora, partidaId, juegoHuerfano, null, null, "{}"));

        var client = _factory.CreateClientConRoles("Operador");
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entrada = EntradaDe(doc, "JuegoActivado");
        Assert.Equal(juegoHuerfano, entrada.GetProperty("juegoId").GetGuid());
        Assert.Equal(JsonValueKind.Null, entrada.GetProperty("juegoOrden").ValueKind);
    }
}
