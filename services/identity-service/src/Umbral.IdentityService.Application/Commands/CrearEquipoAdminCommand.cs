using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CrearEquipoAdminCommand(string NombreEquipo, Guid LiderUserId) : IRequest<EquipoAdminResponse>;
