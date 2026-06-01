using MediatR;

namespace Umbral.TeamService.Application.Teams.CreateTeam;

public sealed record CrearEquipoCommand(Guid ActorUserId, string NombreEquipo) : IRequest<CrearEquipoResponse>;
