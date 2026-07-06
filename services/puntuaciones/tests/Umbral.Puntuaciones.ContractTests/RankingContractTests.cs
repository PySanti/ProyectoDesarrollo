using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.ContractTests;

public class RankingContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public RankingContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task<(Guid partidaId, Guid juegoId, Guid competidorId)> Sembrar()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await sender.Send(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), competidorId, 10, 1500, null));
        return (partidaId, juegoId, competidorId);
    }

    [Fact]
    public async Task Ranking_body_matches_contract()
    {
        var (partidaId, juegoId, _) = await Sembrar();
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(juegoId, root.GetProperty("juegoId").GetGuid());
        Assert.Equal("Trivia", root.GetProperty("tipoJuego").GetString());
        Assert.True(root.TryGetProperty("generadoEn", out _));
        var entrada = root.GetProperty("entradas")[0];
        Assert.Equal(1, entrada.GetProperty("posicion").GetInt32());
        Assert.True(entrada.TryGetProperty("competidorId", out _));
        Assert.Equal("Participante", entrada.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(10, entrada.GetProperty("puntos").GetInt32());
        Assert.Equal(1500, entrada.GetProperty("tiempoAcumuladoMs").GetInt64());
        Assert.Equal(1, entrada.GetProperty("unidadesGanadas").GetInt32());
    }

    [Fact]
    public async Task Marcador_body_matches_contract()
    {
        var (partidaId, juegoId, competidorId) = await Sembrar();
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{competidorId}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(competidorId, root.GetProperty("competidorId").GetGuid());
        Assert.Equal("Participante", root.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(10, root.GetProperty("puntos").GetInt32());
        Assert.Equal(1500, root.GetProperty("tiempoAcumuladoMs").GetInt64());
        Assert.Equal(1, root.GetProperty("unidadesGanadas").GetInt32());
        Assert.Equal(1, root.GetProperty("posicion").GetInt32());
    }

    [Fact]
    public async Task Errores_404_devuelven_message_json()
    {
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/juegos/{Guid.NewGuid()}/ranking");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }
}
