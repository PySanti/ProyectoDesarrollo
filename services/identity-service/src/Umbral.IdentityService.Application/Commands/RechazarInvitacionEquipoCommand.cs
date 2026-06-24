using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record RechazarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitacionId) : IRequest<RechazarInvitacionEquipoResponse>;
