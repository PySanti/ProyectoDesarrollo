using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGamesPublicControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGamesPublicControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_AsParticipante_Returns200WithList()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/trivia-games");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TriviaGameListItemDto>>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetAll_AsOperador_Returns200WithList()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithOperadorRole());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/trivia-games");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TriviaGameListItemDto>>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetAll_NoGames_ReturnsEmptyList()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/trivia-games");
        var body = await response.Content.ReadFromJsonAsync<List<TriviaGameListItemDto>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_FilterByModalidad_Returns200()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/trivia-games?modalidad=Individual");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TriviaGameListItemDto>>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetAll_FilterByInvalidModalidad_Returns400()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/trivia-games?modalidad=Invalido");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
