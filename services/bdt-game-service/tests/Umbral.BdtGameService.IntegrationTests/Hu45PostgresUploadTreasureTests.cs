using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu45PostgresUploadTreasureTests : IClassFixture<PostgresBdtApiFactory>, IAsyncLifetime
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu45PostgresUploadTreasureTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UploadTreasure_Should_Persist_Attempt_With_Npgsql()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var etapaId = partida.Etapas.Single(etapa => etapa.Estado == EstadoEtapa.Activa).EtapaId;

        var response = await _client.SendAsync(CreateUploadRequest(partida.PartidaId, etapaId, participanteId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var tesoro = await dbContext.Set<TesoroQR>().SingleAsync();
        Assert.Equal(partida.PartidaId, tesoro.PartidaId);
        Assert.Equal(etapaId, tesoro.EtapaId);
        Assert.Equal("QR-ETAPA-1", tesoro.QrDecodificado);
        Assert.Equal(EstadoProcesamientoTesoroQr.Decodificado, tesoro.EstadoProcesamiento);
        Assert.NotEqual(default, tesoro.FechaEnvioUtc);
        Assert.NotEmpty(tesoro.ImagenReferencia);
        var storedPath = Path.Combine(
            Path.GetTempPath(),
            "umbral-bdt-game-service",
            "treasure-uploads",
            tesoro.ImagenReferencia.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(storedPath));
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/stages/{etapaId}/treasures");
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(CreateQrPng("QR-ETAPA-1"));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "tesoro.png");
        request.Content = content;
        return request;
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
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
    }
}
