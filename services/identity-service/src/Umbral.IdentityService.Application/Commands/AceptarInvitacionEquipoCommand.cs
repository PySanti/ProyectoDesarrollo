using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record AceptarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitacionId) : IRequest<AceptarInvitacionEquipoResponse>;
