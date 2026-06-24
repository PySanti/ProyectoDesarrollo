using MediatR;
using Umbral.IdentityService.Application.Exceptions;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class SalirDeEquipoCommandHandler : IRequestHandler<SalirDeEquipoCommand, SalirDeEquipoResponse>
{
    private readonly IEquipoRepository _equipoRepository;
    private readonly IInvitacionEquipoRepository _invitacionRepository;

    public SalirDeEquipoCommandHandler(
        IEquipoRepository equipoRepository,
        IInvitacionEquipoRepository invitacionRepository)
    {
        _equipoRepository = equipoRepository;
        _invitacionRepository = invitacionRepository;
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

            if (resultado == ResultadoSalidaEquipo.EquipoEliminado)
            {
                await _invitacionRepository.DeletePendientesByEquipoAsync(equipo.EquipoId, cancellationToken);
            }

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
