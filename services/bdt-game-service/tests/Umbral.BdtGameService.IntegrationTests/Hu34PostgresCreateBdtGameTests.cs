using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu34PostgresCreateBdtGameTests : IClassFixture<PostgresBdtApiFactory>
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu34PostgresCreateBdtGameTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostBdtGames_Should_Persist_Game_And_Stages_With_Npgsql()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.SendAsync(CreatePostRequest(ValidIndividualPayload()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var partida = await dbContext.Partidas.Include(value => value.Etapas).SingleAsync();
        Assert.Equal("Busqueda QR Campus", partida.Nombre);
        Assert.Equal("Patio central y biblioteca", partida.AreaBusqueda.Descripcion);
        Assert.Equal("Individual", partida.Modalidad.ToString());
        Assert.Equal("Lobby", partida.Estado.ToString());
        Assert.Equal(2, partida.MinimoParticipantes);
        Assert.Equal(20, partida.MaximoParticipantes);
        Assert.Null(partida.MaximoEquipos);
        Assert.Null(partida.MinimoJugadoresPorEquipo);
        Assert.Equal("Manual", partida.ModoInicio.ToString());
        Assert.Single(partida.Etapas);
        Assert.Equal(1, partida.Etapas[0].Orden);
        Assert.Equal("QR-ETAPA-1", partida.Etapas[0].CodigoQREsperado);
        Assert.Equal(300, partida.Etapas[0].TiempoLimiteSegundos);
    }

    private static HttpRequestMessage CreatePostRequest(CreateBdtPayload payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bdt/games")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Test-Role", "Operador");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return request;
    }

    private static CreateBdtPayload ValidIndividualPayload()
    {
        return new CreateBdtPayload(
            "Busqueda QR Campus",
            "Patio central y biblioteca",
            "Individual",
            2,
            20,
            null,
            null,
            "Manual",
            new[] { new CreateStagePayload(1, "QR-ETAPA-1", 300) });
    }

    private sealed record CreateBdtPayload(
        string Nombre,
        string AreaBusqueda,
        string Modalidad,
        int MinimoParticipantes,
        int? MaximoParticipantes,
        int? MaximoEquipos,
        int? MinimoJugadoresPorEquipo,
        string ModoInicio,
        IReadOnlyList<CreateStagePayload> Etapas);

    private sealed record CreateStagePayload(int Orden, string CodigoQrEsperado, int TiempoLimiteSegundos);
}
