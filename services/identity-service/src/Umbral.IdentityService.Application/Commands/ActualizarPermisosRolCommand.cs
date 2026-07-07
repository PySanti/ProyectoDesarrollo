using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record ActualizarPermisosRolCommand(string Rol, IReadOnlyList<string> Permisos) : IRequest<RolPermisosDto>;
