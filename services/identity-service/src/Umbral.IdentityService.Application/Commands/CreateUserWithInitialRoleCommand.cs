using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CreateUserWithInitialRoleCommand(
    string Name,
    string Email,
    string InitialRole
) : IRequest<CreateUserWithInitialRoleResponse>;
