using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetParticipantesElegiblesQuery(Guid ActorUserId) : IRequest<IReadOnlyList<ParticipanteElegibleResponse>>;
