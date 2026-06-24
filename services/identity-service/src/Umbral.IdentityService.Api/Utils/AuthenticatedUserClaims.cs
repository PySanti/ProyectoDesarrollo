using System.Security.Claims;

namespace Umbral.IdentityService.Api.Utils;

internal static class AuthenticatedUserClaims
{
    /// <summary>
    /// Extracts the actor user ID from the JWT "sub" claim.
    /// Returns true and sets <paramref name="userId"/> when the claim is present and is a valid Guid.
    /// </summary>
    public static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var sub = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var parsed))
        {
            userId = parsed;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
