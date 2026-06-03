using MediatR;

namespace Umbral.TeamService.Application.Teams.TransferLeadership;

public sealed record TransferirLiderazgoCommand(Guid ActorUserId, Guid NuevoLiderUserId) : IRequest<TransferirLiderazgoResponse>;
