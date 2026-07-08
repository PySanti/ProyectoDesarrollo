using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record ReasignarLiderazgoAdminCommand(Guid EquipoId, Guid NuevoLiderUserId) : IRequest<EquipoAdminResponse>;
