using MediatR;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Domain.Exceptions;

namespace Umbral.TeamService.Application.Teams.LeaveTeam;

public sealed class SalirDeEquipoCommandHandler : IRequestHandler<SalirDeEquipoCommand, SalirDeEquipoResponse>
{
    private readonly IEquipoRepository _equipoRepository;

    public SalirDeEquipoCommandHandler(IEquipoRepository equipoRepository)
    {
        _equipoRepository = equipoRepository;
    }

    public async Task<SalirDeEquipoResponse> Handle(SalirDeEquipoCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipoRepository.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null)
        {
            throw new NoActiveTeamForParticipantException(request.ActorUserId);
        }

        try
        {
            var resultado = equipo.Salir(request.ActorUserId);

            await _equipoRepository.UpdateAsync(equipo, cancellationToken);

            return new SalirDeEquipoResponse(
                request.ActorUserId,
                equipo.EquipoId,
                resultado.ToString(),
                equipo.Estado.ToString());
        }
        catch (LiderDebeTransferirLiderazgoException ex)
        {
            throw new LeaveTeamConflictException(ex.Message);
        }
        catch (ParticipanteNoPerteneceAlEquipoException ex)
        {
            throw new LeaveTeamConflictException(ex.Message);
        }
        catch (EquipoNoActivoException ex)
        {
            throw new LeaveTeamConflictException(ex.Message);
        }
    }
}
