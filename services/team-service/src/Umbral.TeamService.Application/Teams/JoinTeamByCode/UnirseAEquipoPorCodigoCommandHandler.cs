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

        var equipo = await _equipoRepository.GetActiveByAccessCodeAsync(normalizedCode, cancellationToken);
        if (equipo is null)
        {
            // Check if team exists before acquiring lock to avoid unnecessary locking
            throw new TeamNotFoundByAccessCodeException(request.CodigoAcceso);
        }

        // Acquire advisory lock on the team code to prevent concurrent joins
        await _equipoRepository.AcquireAdvisoryLockAsync(normalizedCode, cancellationToken);

        // Re-check conditions after acquiring the lock
        var alreadyInActiveTeam = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.ActorUserId, cancellationToken);
        if (alreadyInActiveTeam)
        {
            throw new AlreadyBelongsToActiveTeamException(request.ActorUserId);
        }

        equipo = await _equipoRepository.GetActiveByAccessCodeAsync(normalizedCode, cancellationToken);
        if (equipo is null)
        {
            throw new TeamNotFoundByAccessCodeException(request.CodigoAcceso);
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
            await _equipoRepository.UpdateAsync(equipo, cancellationToken);
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
    }
}
