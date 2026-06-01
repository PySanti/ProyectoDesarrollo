namespace Umbral.IdentityService.Application.Users.UpdateUserGeneralData;

public sealed record UpdateUserGeneralDataResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Status
);
