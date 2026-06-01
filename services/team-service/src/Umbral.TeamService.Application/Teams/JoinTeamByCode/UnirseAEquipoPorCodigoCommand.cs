using MediatR;

namespace Umbral.TeamService.Application.Teams.JoinTeamByCode;

public sealed record UnirseAEquipoPorCodigoCommand(Guid ActorUserId, string CodigoAcceso)
    : IRequest<UnirseAEquipoPorCodigoResponse>;
