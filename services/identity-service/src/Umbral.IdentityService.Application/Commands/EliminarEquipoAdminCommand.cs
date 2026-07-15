using MediatR;

namespace Umbral.IdentityService.Application.Commands;

public sealed record EliminarEquipoAdminCommand(Guid EquipoId) : IRequest;
