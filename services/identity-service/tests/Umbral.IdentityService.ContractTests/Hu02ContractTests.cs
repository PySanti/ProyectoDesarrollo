using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests;

public sealed class Hu02ContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public Hu02ContractTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_Should_Match_Contract_Status_And_Response_Shape()
    {
        var createResponse = await CreateUserAsAdminAsync(
            "List Contract",
            $"hu02.contract.list.{Guid.NewGuid():N}@umbral.dev",
            "Participante");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/users");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() >= 1);

        var first = document.RootElement[0];
        Assert.True(first.TryGetProperty("userId", out _));
        Assert.True(first.TryGetProperty("keycloakId", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("email", out _));
        Assert.True(first.TryGetProperty("role", out _));
        Assert.True(first.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task GetUserById_Should_Match_Contract_Status_And_Response_Shape()
    {
        var createResponse = await CreateUserAsAdminAsync(
            "Detail Contract",
            $"hu02.contract.detail.{Guid.NewGuid():N}@umbral.dev",
            "Operador");
        var createdUserId = await ExtractUserIdAsync(createResponse);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/identity/users/{createdUserId}");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("keycloakId", out _));
        Assert.True(document.RootElement.TryGetProperty("name", out _));
        Assert.True(document.RootElement.TryGetProperty("email", out _));
        Assert.True(document.RootElement.TryGetProperty("role", out _));
        Assert.True(document.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task UpdateUserGeneralData_Should_Match_Contract_Status_And_Response_Shape()
    {
        var createResponse = await CreateUserAsAdminAsync(
            "Update Contract",
            $"hu02.contract.update.{Guid.NewGuid():N}@umbral.dev",
            "Participante");
        var createdUserId = await ExtractUserIdAsync(createResponse);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/identity/users/{createdUserId}")
        {
            Content = JsonContent.Create(new
            {
                name = "Updated Contract Name",
                email = "hu02.contract.updated@umbral.dev"
            })
        };
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("name", out _));
        Assert.True(document.RootElement.TryGetProperty("email", out _));
        Assert.True(document.RootElement.TryGetProperty("role", out _));
        Assert.True(document.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task DeactivateUser_Should_Match_Contract_Status_And_Response_Shape()
    {
        var createResponse = await CreateUserAsAdminAsync(
            "Deactivate Contract",
            $"hu02.contract.deactivate.{Guid.NewGuid():N}@umbral.dev",
            "Participante");
        var createdUserId = await ExtractUserIdAsync(createResponse);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/identity/users/{createdUserId}/deactivation");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("status", out _));
    }

    private async Task<HttpResponseMessage> CreateUserAsAdminAsync(string name, string email, string initialRole)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/users")
        {
            Content = JsonContent.Create(new
            {
                name,
                email,
                initialRole
            })
        };
        request.Headers.Add("X-Test-Role", "Administrador");

        return await _client.SendAsync(request);
    }

    private static async Task<Guid> ExtractUserIdAsync(HttpResponseMessage createResponse)
    {
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var body = await createResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("userId", out var userIdElement));
        Assert.True(Guid.TryParse(userIdElement.GetString(), out var userId));
        return userId;
    }
}
