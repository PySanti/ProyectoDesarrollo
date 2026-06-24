using MediatR;
using Umbral.IdentityService.Application.Exceptions;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class TransferirLiderazgoCommandHandler : IRequestHandler<TransferirLiderazgoCommand, TransferirLiderazgoResponse>
{
    private readonly IEquipoRepository _equipoRepository;

    public TransferirLiderazgoCommandHandler(IEquipoRepository equipoRepository)
    {
        _equipoRepository = equipoRepository;
    }

    public async Task<TransferirLiderazgoResponse> Handle(TransferirLiderazgoCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipoRepository.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null)
        {
            throw new NoActiveTeamForParticipantException(request.ActorUserId);
        }

        try
        {
            var (liderAnteriorUserId, nuevoLiderUserId) = equipo.TransferirLiderazgo(request.ActorUserId, request.NuevoLiderUserId);

            await _equipoRepository.UpdateAsync(equipo, cancellationToken);

            return new TransferirLiderazgoResponse(
                equipo.EquipoId,
                liderAnteriorUserId,
                nuevoLiderUserId,
                equipo.Estado.ToString());
        }
        catch (ActorNoEsLiderEquipoException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (NuevoLiderNoPerteneceAlEquipoException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (NuevoLiderDebeSerDiferenteException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (ParticipanteNoPerteneceAlEquipoException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (EquipoNoActivoException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
    }
}
