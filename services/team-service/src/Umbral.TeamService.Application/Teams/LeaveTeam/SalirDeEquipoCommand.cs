using MediatR;

namespace Umbral.TeamService.Application.Teams.LeaveTeam;

public sealed record SalirDeEquipoCommand(Guid ActorUserId) : IRequest<SalirDeEquipoResponse>;
