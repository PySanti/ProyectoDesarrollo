using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.Infrastructure.Identity;

public sealed class KeycloakIdentityAdapter : IKeycloakIdentityPort
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakIdentityAdapter(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CreateUserWithInitialRoleAsync(
        string name,
        string email,
        string initialRole,
        CancellationToken cancellationToken)
    {
        ValidateOptions();

        if (string.IsNullOrWhiteSpace(initialRole))
        {
            throw new KeycloakIntegrationException("Initial role is required for Keycloak user creation");
        }

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var userId = await CreateUserAsync(accessToken, name, email, cancellationToken);
            await AssignRealmRoleAsync(accessToken, userId, initialRole, cancellationToken);
            return userId;
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure", ex);
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Realm) ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new KeycloakIntegrationException("Keycloak settings are missing or incomplete");
        }
    }

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/realms/{_options.Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to get Keycloak token. StatusCode={(int)response.StatusCode}");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!payload.TryGetProperty("access_token", out var tokenElement))
        {
            throw new KeycloakIntegrationException("Keycloak token response does not include access_token");
        }

        return tokenElement.GetString() ?? throw new KeycloakIntegrationException("Keycloak returned empty access_token");
    }

    private async Task<string> CreateUserAsync(string accessToken, string name, string email, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            username = email,
            email,
            enabled = true,
            firstName = name,
            requiredActions = _options.RequireUpdatePasswordAction ? new[] { "UPDATE_PASSWORD" } : Array.Empty<string>(),
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = _options.TemporaryPassword,
                    temporary = true
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to create user in Keycloak. StatusCode={(int)response.StatusCode}");
        }

        if (response.Headers.Location is null)
        {
            throw new KeycloakIntegrationException("Keycloak create-user response does not include Location header");
        }

        var location = response.Headers.Location.ToString().TrimEnd('/');
        var userId = location.Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new KeycloakIntegrationException("Unable to parse Keycloak user id from Location header");
        }

        return userId;
    }

    private async Task AssignRealmRoleAsync(string accessToken, string userId, string roleName, CancellationToken cancellationToken)
    {
        var role = await GetRealmRoleAsync(accessToken, roleName, cancellationToken);

        var assignmentBody = new[]
        {
            new
            {
                id = role.Id,
                name = role.Name
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(assignmentBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to assign role in Keycloak. StatusCode={(int)response.StatusCode}");
        }
    }

    private async Task<KeycloakRole> GetRealmRoleAsync(string accessToken, string roleName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to fetch role '{roleName}' from Keycloak. StatusCode={(int)response.StatusCode}");
        }

        var role = await response.Content.ReadFromJsonAsync<KeycloakRole>(cancellationToken: cancellationToken);
        if (role is null || string.IsNullOrWhiteSpace(role.Id) || string.IsNullOrWhiteSpace(role.Name))
        {
            throw new KeycloakIntegrationException($"Keycloak role '{roleName}' response is invalid");
        }

        return role;
    }

    private sealed record KeycloakRole(string Id, string Name);
}
