using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests;

public sealed class CreateUserContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public CreateUserContractTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateUser_Should_Match_Response_Contract()
    {
        var request = new
        {
            name = "Admin",
            email = "admin.contract@umbral.dev",
            initialRole = "Administrador"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("keycloakId", out _));
        Assert.True(document.RootElement.TryGetProperty("name", out _));
        Assert.True(document.RootElement.TryGetProperty("email", out _));
        Assert.True(document.RootElement.TryGetProperty("role", out _));
        Assert.True(document.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_BadRequest_For_Invalid_Payload()
    {
        var request = new
        {
            name = "",
            email = "invalid",
            initialRole = "Guest"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
