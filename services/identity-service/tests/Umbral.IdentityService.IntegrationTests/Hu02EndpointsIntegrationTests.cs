using System.Net;
using System.Net.Http.Json;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class Hu02EndpointsIntegrationTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public Hu02EndpointsIntegrationTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("GET", "/identity/users")]
    [InlineData("GET", "/identity/users/{id}")]
    [InlineData("PATCH", "/identity/users/{id}")]
    [InlineData("PATCH", "/identity/users/{id}/deactivation")]
    public async Task Hu02_Endpoints_Should_Return_Unauthorized_When_Unauthenticated(string method, string rawPath)
    {
        var userId = Guid.NewGuid();
        var path = rawPath.Replace("{id}", userId.ToString());

        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (path.EndsWith("/deactivation", StringComparison.OrdinalIgnoreCase) is false && method == "PATCH")
        {
            request.Content = JsonContent.Create(new { name = "Admin", email = "admin@umbral.dev" });
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/identity/users")]
    [InlineData("GET", "/identity/users/{id}")]
    [InlineData("PATCH", "/identity/users/{id}")]
    [InlineData("PATCH", "/identity/users/{id}/deactivation")]
    public async Task Hu02_Endpoints_Should_Return_Forbidden_For_Non_Admin(string method, string rawPath)
    {
        var userId = Guid.NewGuid();
        var path = rawPath.Replace("{id}", userId.ToString());

        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("X-Test-Role", "Operador");
        if (path.EndsWith("/deactivation", StringComparison.OrdinalIgnoreCase) is false && method == "PATCH")
        {
            request.Content = JsonContent.Create(new { name = "Admin", email = "admin@umbral.dev" });
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_Should_Return_Ok_For_Admin()
    {
        var createResponse = await CreateUserAsAdminAsync("List User", "hu02.list@umbral.dev", "Participante");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Get, "/identity/users");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_Should_Return_Ok_For_Existing_User()
    {
        var createResponse = await CreateUserAsAdminAsync("Detail User", "hu02.detail@umbral.dev", "Participante");
        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/identity/users/{createdUser!.UserId}");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_Should_Return_NotFound_For_Unknown_User()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/identity/users/{Guid.NewGuid()}");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserGeneralData_Should_Return_Ok_For_Valid_Request()
    {
        var createResponse = await CreateUserAsAdminAsync("Update User", "hu02.update@umbral.dev", "Operador");
        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/users/{createdUser!.UserId}")
        {
            Content = JsonContent.Create(new
            {
                name = "Updated Name",
                email = "hu02.update.changed@umbral.dev"
            })
        };
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserGeneralData_Should_Return_NotFound_For_Unknown_User()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/users/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new
            {
                name = "Updated Name",
                email = "hu02.notfound@umbral.dev"
            })
        };
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserGeneralData_Should_Return_Conflict_For_Duplicate_Email()
    {
        var createFirstResponse = await CreateUserAsAdminAsync("First User", "hu02.dup.first@umbral.dev", "Participante");
        var createSecondResponse = await CreateUserAsAdminAsync("Second User", "hu02.dup.second@umbral.dev", "Participante");

        var first = await createFirstResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        var second = await createSecondResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/users/{first!.UserId}")
        {
            Content = JsonContent.Create(new
            {
                name = "Updated Name",
                email = second!.Email
            })
        };
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateUser_Should_Return_Ok_For_Existing_User()
    {
        var createResponse = await CreateUserAsAdminAsync("Deactivate User", "hu02.deactivate@umbral.dev", "Participante");
        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/users/{createdUser!.UserId}/deactivation");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateUser_Should_Return_NotFound_For_Unknown_User()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/users/{Guid.NewGuid()}/deactivation");
        request.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateUserAsAdminAsync(string name, string email, string initialRole)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
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

    private sealed record CreateUserResponse(Guid UserId, string KeycloakId, string Name, string Email, string Role, string Status);
}
