using MediatR;

namespace Umbral.IdentityService.Application.Users.CreateUser;

public sealed record CreateUserWithInitialRoleCommand(
    string Name,
    string Email,
    string InitialRole
) : IRequest<CreateUserWithInitialRoleResponse>;
