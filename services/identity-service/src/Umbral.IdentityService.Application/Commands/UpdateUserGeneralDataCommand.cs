using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record UpdateUserGeneralDataCommand(
    Guid UserId,
    string Name,
    string Email
) : IRequest<UpdateUserGeneralDataResponse>;
