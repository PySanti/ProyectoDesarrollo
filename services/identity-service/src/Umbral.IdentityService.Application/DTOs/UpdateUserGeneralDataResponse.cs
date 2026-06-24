namespace Umbral.IdentityService.Application.DTOs;

public sealed record UpdateUserGeneralDataResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Status
);
