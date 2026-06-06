using System.Security.Claims;

namespace Umbral.BdtGameService.Api.Authentication;

internal static class AuthenticatedUserClaims
{
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var userIdClaim = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}
