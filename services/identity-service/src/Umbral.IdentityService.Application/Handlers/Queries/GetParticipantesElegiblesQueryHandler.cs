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

    public GetParticipantesElegiblesQueryHandler(
        IEquipoRepository equipoRepository,
        IUsuarioRepository usuarioRepository)
    {
        _equipoRepository = equipoRepository;
        _usuarioRepository = usuarioRepository;
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

        var miembrosActuales = equipo.Participantes.Select(p => p.UsuarioId).ToHashSet();

        var todosLosUsuarios = await _usuarioRepository.GetAllAsync(cancellationToken);

        // Los competidores viajan en el espacio del sub de Keycloak, no del UsuarioId local:
        // ParticipanteEquipo.UsuarioId guarda el sub, y con el sub llega el actor en el token.
        // El join va por KeycloakId parseado (mismo patron que ListarEquiposQueryHandler y
        // ResolverNombresQueryHandler). Un KeycloakId no parseable se ignora.
        //
        // El id que sale de aqui es con el que el lider crea la invitacion y con el que el
        // invitado la busca desde su token: devolver el UsuarioId local la archivaba bajo un id
        // que nadie vuelve a presentar, y ademas anulaba los dos guardas de abajo.
        var result = new List<ParticipanteElegibleResponse>();

        foreach (var usuario in todosLosUsuarios)
        {
            if (usuario.Rol != RolUsuario.Participante)
                continue;

            if (!Guid.TryParse(usuario.KeycloakId, out var sub))
                continue;

            if (miembrosActuales.Contains(sub))
                continue;

            var tieneEquipoActivo = await _equipoRepository.ExistsActiveTeamByUserIdAsync(sub, cancellationToken);
            if (tieneEquipoActivo)
                continue;

            result.Add(new ParticipanteElegibleResponse(
                sub,
                usuario.Nombre,
                usuario.Correo));
        }

        return result;
    }
}
