using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<DeactivateUserResponse>;
