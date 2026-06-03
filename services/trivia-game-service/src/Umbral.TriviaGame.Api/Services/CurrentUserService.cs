using System.Security.Claims;
using Umbral.TriviaGame.Application.Ports;

namespace Umbral.TriviaGame.Api.Services;

/// <summary>
/// Implementación concreta de ICurrentUserService que extrae el OperatorId
/// del claim "sub" del JWT autenticado. En ausencia de Keycloak, usa un
/// valor por defecto configurable o lo obtiene de un header de desarrollo.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string OperatorId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var subClaim = user.FindFirst("sub")?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(subClaim))
                    return subClaim;
            }

            return "system";
        }
    }
}
