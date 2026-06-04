using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu45ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu45ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadTreasure_Should_Match_Hu45_Response_Shape()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var response = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("tesoroId", out _));
        Assert.True(document.RootElement.TryGetProperty("partidaId", out _));
        Assert.True(document.RootElement.TryGetProperty("etapaId", out _));
        Assert.True(document.RootElement.TryGetProperty("exploradorId", out _));
        Assert.True(document.RootElement.TryGetProperty("fechaEnvioUtc", out _));
        Assert.True(document.RootElement.TryGetProperty("estadoProcesamiento", out _));
        Assert.True(document.RootElement.TryGetProperty("qrDecodificado", out _));
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Theory]
    [InlineData("bad", "00000000-0000-0000-0000-000000000001", HttpStatusCode.BadRequest)]
    [InlineData("00000000-0000-0000-0000-000000000001", "bad", HttpStatusCode.BadRequest)]
    public async Task UploadTreasure_Should_Match_BadRequest_Status(string partidaId, string etapaId, HttpStatusCode expected)
    {
        var response = await _client.SendAsync(CreateUploadRequest(partidaId, etapaId, Guid.NewGuid()));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Match_Unauthorized_Status()
    {
        using var content = CreateMultipartContent("image/jpeg", Encoding.UTF8.GetBytes("QR:QR"));

        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/stages/{Guid.NewGuid()}/treasures", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("text/plain", HttpStatusCode.UnsupportedMediaType)]
    [InlineData("image/jpeg", HttpStatusCode.RequestEntityTooLarge)]
    public async Task UploadTreasure_Should_Match_Image_Constraint_Statuses(string contentType, HttpStatusCode expected)
    {
        var bytes = expected == HttpStatusCode.RequestEntityTooLarge
            ? new byte[(5 * 1024 * 1024) + 1]
            : Encoding.UTF8.GetBytes("QR:QR");

        var response = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid(), contentType, bytes));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Match_NotFound_And_Conflict_Error_Shapes()
    {
        await ClearDatabaseAsync();
        var notFound = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid()));
        var notFoundBody = await notFound.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        using (var document = JsonDocument.Parse(notFoundBody))
        {
            Assert.True(document.RootElement.TryGetProperty("message", out _));
        }

        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);
        var conflict = await _client.SendAsync(CreateUploadRequest(partida.PartidaId.ToString(), partida.Etapas.Single().EtapaId.ToString(), participanteId));
        var conflictBody = await conflict.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using (var document = JsonDocument.Parse(conflictBody))
        {
            Assert.True(document.RootElement.TryGetProperty("message", out _));
        }
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        dbContext.Set<TesoroQR>().RemoveRange(dbContext.Set<TesoroQR>());
        dbContext.Set<ExploradorBDT>().RemoveRange(dbContext.Set<ExploradorBDT>());
        dbContext.Partidas.RemoveRange(dbContext.Partidas);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateUploadRequest(Guid partidaId, Guid etapaId, Guid userId)
    {
        return CreateUploadRequest(partidaId.ToString(), etapaId.ToString(), userId);
    }

    private static HttpRequestMessage CreateUploadRequest(string partidaId, string etapaId, Guid userId, string contentType = "image/jpeg", byte[]? bytes = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/stages/{etapaId}/treasures");
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Content = CreateMultipartContent(contentType, bytes ?? Encoding.UTF8.GetBytes("QR:QR-ETAPA-1"));
        return request;
    }

    private static MultipartFormDataContent CreateMultipartContent(string contentType, byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(bytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "image", contentType == "image/png" ? "tesoro.png" : "tesoro.jpg");
        return content;
    }

    private static PartidaBDT CreateStartedIndividualGame(Guid participanteId)
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
    }

    private static PartidaBDT CreateIndividualGame()
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }
}
