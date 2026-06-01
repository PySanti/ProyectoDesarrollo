using MediatR;

namespace Umbral.IdentityService.Application.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<DeactivateUserResponse>;
