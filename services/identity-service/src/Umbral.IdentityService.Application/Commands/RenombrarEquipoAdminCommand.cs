using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record RenombrarEquipoAdminCommand(Guid EquipoId, string NombreEquipo) : IRequest<EquipoAdminResponse>;
