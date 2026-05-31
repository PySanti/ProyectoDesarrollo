namespace Umbral.IdentityService.Application.Users.CreateUser;

public sealed record CreateUserWithInitialRoleResponse(
    Guid UserId,
    string KeycloakId,
    string Name,
    string Email,
    string Role,
    string Status
);
