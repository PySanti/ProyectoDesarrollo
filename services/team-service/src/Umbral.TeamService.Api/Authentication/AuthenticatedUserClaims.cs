using System.Security.Claims;

namespace Umbral.TeamService.Api.Authentication;

internal static class AuthenticatedUserClaims
{
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        // Keycloak emite el id del usuario en "sub". Con MapInboundClaims=true
        // (por defecto en .NET) ese claim se renombra a ClaimTypes.NameIdentifier,
        // por eso hay que buscar ambos: si solo se busca "sub" devuelve null y el
        // handler responde 403 aunque el usuario sea un Participante valido.
        var userIdClaim = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}
