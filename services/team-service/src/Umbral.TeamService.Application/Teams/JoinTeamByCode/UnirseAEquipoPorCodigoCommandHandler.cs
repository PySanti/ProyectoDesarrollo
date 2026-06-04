using MediatR;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;

namespace Umbral.TeamService.Application.Teams.JoinTeamByCode;

public sealed class UnirseAEquipoPorCodigoCommandHandler : IRequestHandler<UnirseAEquipoPorCodigoCommand, UnirseAEquipoPorCodigoResponse>
{
    private const int MaximoIntegrantes = 5;

    private readonly IEquipoRepository _equipoRepository;

    public UnirseAEquipoPorCodigoCommandHandler(IEquipoRepository equipoRepository)
    {
        _equipoRepository = equipoRepository;
    }

    public async Task<UnirseAEquipoPorCodigoResponse> Handle(UnirseAEquipoPorCodigoCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.CodigoAcceso.Trim().ToUpperInvariant();

        return await _equipoRepository.ExecuteWithAccessCodeLockAsync(
            normalizedCode,
            async ct =>
            {
                var equipo = await _equipoRepository.GetActiveByAccessCodeAsync(normalizedCode, ct);
                if (equipo is null)
                {
                    throw new TeamNotFoundByAccessCodeException(request.CodigoAcceso);
                }

                var alreadyInActiveTeam = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.ActorUserId, ct);
                if (alreadyInActiveTeam)
                {
                    throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
                }

                if (equipo.Participantes.Count >= MaximoIntegrantes)
                {
                    throw new TeamFullException(equipo.EquipoId);
                }

                if (equipo.Participantes.Any(x => x.UsuarioId == request.ActorUserId))
                {
                    throw new ParticipantAlreadyInTargetTeamException(equipo.EquipoId, request.ActorUserId);
                }

                equipo.AgregarParticipante(request.ActorUserId);

                try
                {
                    await _equipoRepository.UpdateAsync(equipo, ct);
                }
                catch (UniqueMembershipConflictException)
                {
                    throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
                }

                var integrantes = equipo.Participantes
                    .Select(x => new UnirseAEquipoIntegranteResponse(x.UsuarioId, x.EsLider))
                    .ToArray();

                var liderUserId = equipo.Participantes.Single(x => x.EsLider).UsuarioId;

                return new UnirseAEquipoPorCodigoResponse(
                    equipo.EquipoId,
                    equipo.NombreEquipo,
                    equipo.CodigoAcceso,
                    equipo.Estado.ToString(),
                    liderUserId,
                    integrantes);
            },
            cancellationToken);
    }
}
