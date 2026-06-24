namespace Umbral.IdentityService.Application.DTOs;

public sealed record DeactivateUserResponse(
    Guid UserId,
    string Status
);
