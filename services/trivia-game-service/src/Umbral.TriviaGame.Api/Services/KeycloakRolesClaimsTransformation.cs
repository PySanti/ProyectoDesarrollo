using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Umbral.TriviaGame.Api.Services;

public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        var resourceAccessClaim = principal.FindFirst("resource_access")?.Value;
        if (resourceAccessClaim == null)
            return Task.FromResult(principal);

        using var doc = JsonDocument.Parse(resourceAccessClaim);
        var clientRoles = doc.RootElement
            .EnumerateObject()
            .SelectMany(client => client.Value.TryGetProperty("roles", out var roles)
                ? roles.EnumerateArray().Select(r => r.GetString()).Where(r => r != null).Cast<string>()
                : Enumerable.Empty<string>())
            .ToList();

        if (clientRoles.Count == 0)
            return Task.FromResult(principal);

        var identity = principal.Identities.FirstOrDefault() ?? new ClaimsIdentity();
        foreach (var role in clientRoles)
        {
            if (!principal.HasClaim(ClaimTypes.Role, role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return Task.FromResult(principal);
    }
}
