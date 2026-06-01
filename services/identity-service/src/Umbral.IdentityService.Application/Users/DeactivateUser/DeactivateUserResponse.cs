namespace Umbral.IdentityService.Application.Users.DeactivateUser;

public sealed record DeactivateUserResponse(
    Guid UserId,
    string Status
);
