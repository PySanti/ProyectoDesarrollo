using System.Net;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Infrastructure.Services.Identity;

namespace Umbral.IdentityService.UnitTests;

public sealed class KeycloakIdentityAdapterTests
{
    [Fact]
    public async Task CreateUserWithInitialRoleAsync_Should_Throw_DuplicateEmailException_When_Keycloak_Returns_Conflict()
    {
        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"admin-token\"}")
            },
            new HttpResponseMessage(HttpStatusCode.Conflict));

        var adapter = new KeycloakIdentityAdapter(
            new HttpClient(handler),
            Options.Create(new KeycloakOptions
            {
                BaseUrl = "http://keycloak.test",
                Realm = "UMBRAL-UCAB",
                ClientId = "identity-admin",
                ClientSecret = "secret"
            }));

        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            adapter.CreateUserWithInitialRoleAsync("Ana", "ana@test.com", "Participante", "Temp-Pass-1", CancellationToken.None));
    }

    [Fact]
    public async Task SyncUserProfileAsync_Should_Send_Username_Email_And_FirstName()
    {
        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"admin-token\"}")
            },
            new HttpResponseMessage(HttpStatusCode.NoContent));

        var adapter = new KeycloakIdentityAdapter(
            new HttpClient(handler),
            Options.Create(new KeycloakOptions
            {
                BaseUrl = "http://keycloak.test",
                Realm = "UMBRAL-UCAB",
                ClientId = "identity-admin",
                ClientSecret = "secret"
            }));

        await adapter.SyncUserProfileAsync("kc-1", "Ana Perez", "ana@test.com", CancellationToken.None);

        var body = handler.CapturedBodies[^1];
        // Keycloak admite iniciar sesión por username o por email: si el username no sigue al correo,
        // el correo anterior seguiría siendo una credencial válida para siempre.
        Assert.Contains("\"username\":\"ana@test.com\"", body);
        Assert.Contains("\"email\":\"ana@test.com\"", body);
        Assert.Contains("\"firstName\":\"Ana Perez\"", body);
    }

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public List<string> CapturedBodies { get; } = [];

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for HTTP request.");
            }

            CapturedBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responses.Dequeue();
        }
    }
}
