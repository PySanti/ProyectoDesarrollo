using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CambiarEstadoEquipoAdminCommand(Guid EquipoId, string Estado) : IRequest<EquipoAdminResponse>;
