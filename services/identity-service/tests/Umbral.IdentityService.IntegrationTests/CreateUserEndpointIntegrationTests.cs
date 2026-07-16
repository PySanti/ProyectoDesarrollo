using System.Net;
using System.Net.Http.Json;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class CreateUserEndpointIntegrationTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public CreateUserEndpointIntegrationTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_Created_For_Admin()
    {
        var request = new
        {
            name = "Admin",
            email = "admin.integration@umbral.dev",
            initialRole = "Administrador"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Test-Role", "Administrador");

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_Forbidden_For_NonAdmin()
    {
        var request = new
        {
            name = "Operator",
            email = "operator.integration@umbral.dev",
            initialRole = "Operador"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Test-Role", "Operador");

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_Conflict_For_Duplicate_Email()
    {
        var request = new
        {
            name = "Admin",
            email = "duplicate.integration@umbral.dev",
            initialRole = "Administrador"
        };

        var firstRequest = BuildAdminCreateUserRequest(request);
        var firstResponse = await _client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateRequest = BuildAdminCreateUserRequest(request);
        var duplicateResponse = await _client.SendAsync(duplicateRequest);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_BadGateway_For_Keycloak_Failure_And_Not_Persist_User()
    {
        using var factory = new FailingKeycloakIdentityApiFactory();
        using var client = factory.CreateClient();

        var request = BuildAdminCreateUserRequest(new
        {
            name = "Admin",
            email = "keycloak.failure@umbral.dev",
            initialRole = "Administrador"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(0, factory.GetPersistedUsersCount());
    }

    [Fact]
    public async Task Post_CreateUser_Should_Return_Unauthorized_When_Request_Has_No_Authentication()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(new
            {
                name = "Admin",
                email = "unauthorized.integration@umbral.dev",
                initialRole = "Administrador"
            })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage BuildAdminCreateUserRequest(object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/identity/users")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Test-Role", "Administrador");
        return request;
    }

    private sealed class FailingKeycloakIdentityApiFactory : IdentityApiFactory
    {
        protected override IKeycloakIdentityPort CreateKeycloakIdentityPort()
        {
            return new FailingKeycloakIdentityPort();
        }
    }

    private sealed class FailingKeycloakIdentityPort : IKeycloakIdentityPort
    {
        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken cancellationToken)
        {
            throw new KeycloakIntegrationException("forced keycloak failure for integration test");
        }

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task SyncUserProfileAsync(string keycloakId, string nombre, string correo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
