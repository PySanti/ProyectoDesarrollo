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

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for HTTP request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
