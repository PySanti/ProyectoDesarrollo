namespace Umbral.IdentityService.Application.Users.Common;

public sealed record UserSummaryResponse(
    Guid UserId,
    string KeycloakId,
    string Name,
    string Email,
    string Role,
    string Status
);
