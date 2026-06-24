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

        var candidatos = todosLosUsuarios
            .Where(u => u.Rol == RolUsuario.Participante && !miembrosActuales.Contains(u.UsuarioId))
            .ToList();

        var result = new List<ParticipanteElegibleResponse>(candidatos.Count);

        foreach (var usuario in candidatos)
        {
            var tieneEquipoActivo = await _equipoRepository.ExistsActiveTeamByUserIdAsync(usuario.UsuarioId, cancellationToken);
            if (!tieneEquipoActivo)
            {
                result.Add(new ParticipanteElegibleResponse(
                    usuario.UsuarioId,
                    usuario.Nombre,
                    usuario.Correo));
            }
        }

        return result;
    }
}
