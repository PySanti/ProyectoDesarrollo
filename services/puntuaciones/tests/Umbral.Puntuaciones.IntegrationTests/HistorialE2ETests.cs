using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    public HistorialE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    private Task RegistrarHistorial(
        Guid partidaId, string tipo, DateTime occurredAt,
        Guid? participanteId = null, string detalle = "{}")
        => Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), tipo, occurredAt, partidaId, null, participanteId, null, detalle));

    [Fact]
    public async Task Historial_de_partida_ordenado_paginado_y_filtrado()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));
        await RegistrarHistorial(partidaId, "PartidaIniciada", Ahora.AddMinutes(1), detalle: """{"primerJuegoOrden":1}""");
        await RegistrarHistorial(partidaId, "PartidaPublicadaEnLobby", Ahora);
        await RegistrarHistorial(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(2), detalle: """{"puntaje":10}""");

        // HistorialController ahora exige rol AND privilegio (Task 5); Operador trae
        // GestionarPartidas por default.
        var client = _factory.CreateClientConRoles("Operador", "GestionarPartidas");

        var completo = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial");
        using var jsonCompleto = JsonDocument.Parse(await completo.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, completo.StatusCode);
        Assert.Equal(3, jsonCompleto.RootElement.GetProperty("total").GetInt32());
        var entradas = jsonCompleto.RootElement.GetProperty("entradas");
        Assert.Equal("PartidaPublicadaEnLobby", entradas[0].GetProperty("tipoEvento").GetString());
        Assert.Equal("EtapaBDTGanada", entradas[2].GetProperty("tipoEvento").GetString());
        Assert.Equal(10, entradas[2].GetProperty("detalle").GetProperty("puntaje").GetInt32());

        var paginado = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?limit=1&offset=1");
        using var jsonPaginado = JsonDocument.Parse(await paginado.Content.ReadAsStringAsync());
        Assert.Equal(3, jsonPaginado.RootElement.GetProperty("total").GetInt32());
        Assert.Equal("PartidaIniciada",
            jsonPaginado.RootElement.GetProperty("entradas")[0].GetProperty("tipoEvento").GetString());

        var filtrado = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?tipo=EtapaBDTGanada");
        using var jsonFiltrado = JsonDocument.Parse(await filtrado.Content.ReadAsStringAsync());
        Assert.Equal(1, jsonFiltrado.RootElement.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Muestreo_de_ubicaciones_de_punta_a_punta()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(
            Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora, participanteId);
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora.AddSeconds(30), participanteId);   // descartada
        await RegistrarHistorial(partidaId, "UbicacionActualizada", Ahora.AddSeconds(90), participanteId);   // guardada

        // Administrador solo, sin GestionarPartidas, ya no pasaría; el rol de operación que trae
        // ese privilegio por default es Operador.
        var client = _factory.CreateClientConRoles("Operador", "GestionarPartidas");
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/historial?tipo=UbicacionActualizada");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(2, json.RootElement.GetProperty("total").GetInt32());
    }
}
