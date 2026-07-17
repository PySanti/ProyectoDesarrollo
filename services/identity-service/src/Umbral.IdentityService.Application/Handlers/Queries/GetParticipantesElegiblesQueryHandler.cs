using MediatR;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetParticipantesElegiblesQueryHandler : IRequestHandler<GetParticipantesElegiblesQuery, IReadOnlyList<ParticipanteElegibleResponse>>
{
    private readonly IEquipoRepository _equipoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IInvitacionEquipoRepository _invitacionRepository;

    public GetParticipantesElegiblesQueryHandler(
        IEquipoRepository equipoRepository,
        IUsuarioRepository usuarioRepository,
        IInvitacionEquipoRepository invitacionRepository)
    {
        _equipoRepository = equipoRepository;
        _usuarioRepository = usuarioRepository;
        _invitacionRepository = invitacionRepository;
    }

    public async Task<IReadOnlyList<ParticipanteElegibleResponse>> Handle(GetParticipantesElegiblesQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipoRepository.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);

        if (equipo is null)
            throw new NoEsLiderException(request.ActorUserId);

        var lider = equipo.Participantes.SingleOrDefault(p => p.EsLider);
        if (lider is null || lider.UsuarioId != request.ActorUserId)
            throw new NoEsLiderException(request.ActorUserId);

        if (equipo.Participantes.Count >= 5)
            return Array.Empty<ParticipanteElegibleResponse>();

        // La pertenencia a un equipo se indexa por el sub de Keycloak: ParticipanteEquipo.UsuarioId
        // guarda el sub, no el UsuarioId local (ver ListarEquiposQueryHandler). Un candidato es un
        // Usuario local, cuyo sub vive en KeycloakId; hay que comparar y devolver ESE sub, no el id
        // local, o si no ni se excluye al lider/miembros ni la invitacion resultante es aceptable.
        var miembrosActuales = equipo.Participantes.Select(p => p.UsuarioId).ToHashSet();

        // Los ya invitados NO se excluyen (siguen apareciendo para marcarlos con YaInvitado); solo
        // se filtran miembros y usuarios con equipo activo. El set son subs de Keycloak (InvitadoUserId).
        var invitadosPendientes = (await _invitacionRepository
            .GetInvitadoUserIdsPendientesByEquipoAsync(equipo.EquipoId, cancellationToken)).ToHashSet();

        var todosLosUsuarios = await _usuarioRepository.GetAllAsync(cancellationToken);

        var result = new List<ParticipanteElegibleResponse>();

        foreach (var usuario in todosLosUsuarios)
        {
            if (usuario.Rol != RolUsuario.Participante)
                continue;

            if (!Guid.TryParse(usuario.KeycloakId, out var keycloakSub))
                continue;

            if (miembrosActuales.Contains(keycloakSub))
                continue;

            var tieneEquipoActivo = await _equipoRepository.ExistsActiveTeamByUserIdAsync(keycloakSub, cancellationToken);
            if (tieneEquipoActivo)
                continue;

            result.Add(new ParticipanteElegibleResponse(
                keycloakSub,
                usuario.Nombre,
                usuario.Correo,
                invitadosPendientes.Contains(keycloakSub)));
        }

        return result;
    }
}
