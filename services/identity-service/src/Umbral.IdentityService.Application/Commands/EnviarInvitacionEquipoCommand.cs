using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record EnviarInvitacionEquipoCommand(Guid ActorUserId, Guid InvitadoUserId) : IRequest<EnviarInvitacionEquipoResponse>;
