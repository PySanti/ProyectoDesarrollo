using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Domain.Entities;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu34ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly HttpClient _client;

    public Hu34ContractTests(BdtApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_Hu34_Response_Shape()
    {
        var response = await _client.SendAsync(CreatePostRequest(ValidIndividualPayload()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("partidaId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombre", out _));
        Assert.True(document.RootElement.TryGetProperty("modalidad", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("areaBusqueda", out _));
        Assert.True(document.RootElement.TryGetProperty("modoInicio", out _));
        Assert.True(document.RootElement.TryGetProperty("cantidadEtapas", out _));
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_Unauthenticated_Contract_Status()
    {
        var response = await _client.PostAsJsonAsync("/api/bdt/games", ValidIndividualPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_Forbidden_Contract_Status()
    {
        var response = await _client.SendAsync(CreatePostRequest(ValidIndividualPayload(), role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_BadRequest_Error_Shape()
    {
        var payload = ValidIndividualPayload() with { Etapas = Array.Empty<CreateStagePayload>() };

        var response = await _client.SendAsync(CreatePostRequest(payload));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_Conflict_Contract_Status()
    {
        var payload = ValidIndividualPayload() with { MaximoParticipantes = null };

        var response = await _client.SendAsync(CreatePostRequest(payload));

        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task PostBdtGames_Should_Match_Persistence_Error_Shape()
    {
        await using var factory = new FailingRepositoryBdtApiFactory();
        var client = factory.CreateClient();

        var response = await client.SendAsync(CreatePostRequest(ValidIndividualPayload()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("No se pudo crear la partida BDT.", document.RootElement.GetProperty("message").GetString());
    }

    private static HttpRequestMessage CreatePostRequest(CreateBdtPayload payload, string role = "Operador")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bdt/games")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Test-Role", role);
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

    private sealed class FailingRepositoryBdtApiFactory : BdtApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPartidaBdtRepository>();
                services.AddScoped<IPartidaBdtRepository, FailingPartidaBdtRepository>();
            });
        }
    }

    private sealed class FailingPartidaBdtRepository : IPartidaBdtRepository
    {
        public Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            throw new DbUpdateException("Simulated persistence failure.");
        }

        public Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(
            Guid partidaId,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            throw new DbUpdateException("Simulated persistence failure.");
        }

        public Task<PartidaBDT?> GetByIdWithExploradoresAsync(Guid partidaId, CancellationToken cancellationToken)
        {
            throw new DbUpdateException("Simulated persistence failure.");
        }

        public Task UpdateAsync(PartidaBDT partida, CancellationToken cancellationToken)
        {
            throw new DbUpdateException("Simulated persistence failure.");
        }
    }
}
