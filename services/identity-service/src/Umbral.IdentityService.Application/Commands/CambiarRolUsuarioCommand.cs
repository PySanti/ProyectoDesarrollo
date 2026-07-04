using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CambiarRolUsuarioCommand(Guid UserId, string Rol) : IRequest<CambiarRolUsuarioResponse>;
