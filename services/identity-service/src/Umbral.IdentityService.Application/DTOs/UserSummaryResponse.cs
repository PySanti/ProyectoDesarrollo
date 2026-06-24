namespace Umbral.IdentityService.Application.DTOs;

public sealed record UserSummaryResponse(
    Guid UserId,
    string KeycloakId,
    string Name,
    string Email,
    string Role,
    string Status
);
