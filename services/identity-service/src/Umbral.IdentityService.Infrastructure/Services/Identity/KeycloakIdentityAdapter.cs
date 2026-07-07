using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.Infrastructure.Services.Identity;

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
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        ValidateOptions();

        if (string.IsNullOrWhiteSpace(initialRole))
        {
            throw new KeycloakIntegrationException("Initial role is required for Keycloak user creation");
        }

        if (string.IsNullOrWhiteSpace(temporaryPassword))
        {
            throw new KeycloakIntegrationException("Temporary password is required for Keycloak user creation");
        }

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var userId = await CreateUserAsync(accessToken, name, email, temporaryPassword, cancellationToken);
            await AssignRealmRoleAsync(accessToken, userId, initialRole, cancellationToken);
            return userId;
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (DuplicateEmailException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure", ex);
        }
    }

    public async Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
    {
        ValidateOptions();

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            return;
        }

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // 404 = el usuario ya no existe: la compensación es idempotente.
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new KeycloakIntegrationException($"Failed to delete user in Keycloak. StatusCode={(int)response.StatusCode}");
            }
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure during user deletion", ex);
        }
    }

    public async Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
    {
        ValidateOptions();

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            return false;
        }

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakIntegrationException($"Failed to fetch user from Keycloak. StatusCode={(int)response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            if (payload.TryGetProperty("requiredActions", out var actions) && actions.ValueKind == JsonValueKind.Array)
            {
                foreach (var action in actions.EnumerateArray())
                {
                    if (string.Equals(action.GetString(), "UPDATE_PASSWORD", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure while reading user", ex);
        }
    }

    public async Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken)
    {
        ValidateOptions();

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

            // Partial update: Keycloak fusiona la representación enviada; el resto de atributos
            // del usuario (enabled, roles, requiredActions) se conservan.
            var requestBody = new { email };

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}")
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new DuplicateEmailException(email);
                }

                throw new KeycloakIntegrationException($"Failed to update user email in Keycloak. StatusCode={(int)response.StatusCode}");
            }
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (DuplicateEmailException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure while updating email", ex);
        }
    }

    public async Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
    {
        ValidateOptions();

        if (string.IsNullOrWhiteSpace(temporaryPassword))
        {
            throw new KeycloakIntegrationException("Temporary password is required for Keycloak password reset");
        }

        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

            // temporary=true reinstaura la acción requerida UPDATE_PASSWORD para el próximo login.
            var requestBody = new
            {
                type = "password",
                value = temporaryPassword,
                temporary = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}/reset-password")
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakIntegrationException($"Failed to reset password in Keycloak. StatusCode={(int)response.StatusCode}");
            }
        }
        catch (KeycloakIntegrationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException("Unexpected Keycloak integration failure while resetting password", ex);
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

    private async Task<string> CreateUserAsync(string accessToken, string name, string email, string temporaryPassword, CancellationToken cancellationToken)
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
                    value = temporaryPassword,
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
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new DuplicateEmailException(email);
            }

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

    public async Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var composite = await GetRealmRoleAsync(accessToken, compositeRoleName, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}/composites")
        {
            Content = JsonContent.Create(new[] { new { id = composite.Id, name = composite.Name } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to add composite '{compositeRoleName}' to role '{roleName}'. StatusCode={(int)response.StatusCode}");
        }
    }

    public async Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var composite = await GetRealmRoleAsync(accessToken, compositeRoleName, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}/composites")
        {
            Content = JsonContent.Create(new[] { new { id = composite.Id, name = composite.Name } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        // 404 tolerado: quitar algo ya ausente es idempotente (camino de reparación tras 502 parcial).
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new KeycloakIntegrationException($"Failed to remove composite '{compositeRoleName}' from role '{roleName}'. StatusCode={(int)response.StatusCode}");
        }
    }

    public async Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        var oldRole = await GetRealmRoleAsync(accessToken, oldRoleName, cancellationToken);
        using (var removeRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { new { id = oldRole.Id, name = oldRole.Name } })
        })
        {
            removeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var removeResponse = await _httpClient.SendAsync(removeRequest, cancellationToken);
            // 404 tolerado: el mapping viejo puede no existir (reintento tras fallo parcial).
            if (!removeResponse.IsSuccessStatusCode && removeResponse.StatusCode != HttpStatusCode.NotFound)
            {
                throw new KeycloakIntegrationException($"Failed to remove realm role '{oldRoleName}' from user. StatusCode={(int)removeResponse.StatusCode}");
            }
        }

        await AssignRealmRoleAsync(accessToken, keycloakId, newRoleName, cancellationToken);
    }

    private sealed record KeycloakRole(string Id, string Name);
}
