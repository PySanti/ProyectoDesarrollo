using MediatR;

namespace Umbral.IdentityService.Application.Users.UpdateUserGeneralData;

public sealed record UpdateUserGeneralDataCommand(
    Guid UserId,
    string Name,
    string Email
) : IRequest<UpdateUserGeneralDataResponse>;
