using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record EliminarEquipoAdminCommand(Guid EquipoId) : IRequest<EliminarEquipoAdminResponse>;
