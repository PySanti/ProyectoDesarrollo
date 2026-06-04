using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu45UploadTreasureIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu45UploadTreasureIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadTreasure_Should_Record_Decoded_Qr_Attempt()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var response = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId, "image/png", CreateQrPng("QR-ETAPA-1")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(partida.PartidaId, document.RootElement.GetProperty("partidaId").GetGuid());
        Assert.Equal(etapaId, document.RootElement.GetProperty("etapaId").GetGuid());
        Assert.Equal("Decodificado", document.RootElement.GetProperty("estadoProcesamiento").GetString());
        Assert.Equal("QR-ETAPA-1", document.RootElement.GetProperty("qrDecodificado").GetString());
        Assert.Equal(1, await CountTesorosAsync());
    }

    [Fact]
    public async Task UploadTreasure_Should_Record_Unreadable_Qr_Attempt()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var response = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId, "image/png", new byte[] { 1, 2, 3 }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("NoLegible", document.RootElement.GetProperty("estadoProcesamiento").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("qrDecodificado").ValueKind);
        Assert.Equal(1, await CountTesorosAsync());
    }

    [Fact]
    public async Task UploadTreasure_Should_Allow_Multiple_Attempts()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var first = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId));
        var second = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(2, await CountTesorosAsync());
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_BadRequest_For_Invalid_Ids_Or_Missing_Image()
    {
        var invalidPartida = await _client.SendAsync(CreateUploadRequest("invalid", Guid.NewGuid().ToString(), Guid.NewGuid(), includeImage: true));
        var invalidEtapa = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid().ToString(), "invalid", Guid.NewGuid(), includeImage: true));
        var missingImage = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid(), includeImage: false));

        Assert.Equal(HttpStatusCode.BadRequest, invalidPartida.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidEtapa.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingImage.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_Unauthorized_When_Unauthenticated()
    {
        using var content = CreateMultipartContent("image/jpeg", new byte[] { 1 });

        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/stages/{Guid.NewGuid()}/treasures", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_Forbidden_For_NonParticipant_Or_Unregistered_Participant()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var nonParticipant = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId, role: "Operador"));
        var unregistered = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, nonParticipant.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unregistered.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_NotFound_For_Missing_Game()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_Conflict_For_NonInitiated_Game_Or_Wrong_Stage()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var lobbyGame = CreateIndividualGame();
        lobbyGame.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(lobbyGame);
        var lobbyEtapaId = lobbyGame.Etapas.Single().EtapaId;

        var nonInitiated = await _client.SendAsync(CreateUploadRequest(lobbyGame.PartidaId, lobbyEtapaId, participanteId));

        await ClearDatabaseAsync();
        var started = CreateStartedIndividualGame(participanteId);
        await SeedAsync(started);
        var wrongStage = await _client.SendAsync(CreateUploadRequest(started.PartidaId, Guid.NewGuid(), participanteId));

        Assert.Equal(HttpStatusCode.Conflict, nonInitiated.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, wrongStage.StatusCode);
    }

    [Fact]
    public async Task UploadTreasure_Should_Return_UnsupportedMediaType_And_PayloadTooLarge()
    {
        var unsupported = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), contentType: "text/plain"));
        var tooLarge = await _client.SendAsync(CreateUploadRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), contentType: "image/jpeg", bytes: new byte[(5 * 1024 * 1024) + 1]));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, tooLarge.StatusCode);
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

    private async Task<int> CountTesorosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await dbContext.Set<TesoroQR>().CountAsync();
    }

    private static HttpRequestMessage CreateUploadRequest(Guid partidaId, Guid etapaId, Guid userId, string contentType = "image/jpeg", byte[]? bytes = null, string role = "Participante")
    {
        return CreateUploadRequest(partidaId.ToString(), etapaId.ToString(), userId, true, contentType, bytes, role);
    }

    private static HttpRequestMessage CreateUploadRequest(string partidaId, string etapaId, Guid userId, bool includeImage, string contentType = "image/jpeg", byte[]? bytes = null, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/stages/{etapaId}/treasures");
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Content = includeImage ? CreateMultipartContent(contentType, bytes ?? Encoding.UTF8.GetBytes("QR:QR-ETAPA-1")) : new MultipartFormDataContent();
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

    private static byte[] CreateQrPng(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(20);
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
