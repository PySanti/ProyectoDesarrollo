using MediatR;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetInvitacionesRecibidasQueryHandler : IRequestHandler<GetInvitacionesRecibidasQuery, IReadOnlyList<InvitacionRecibidasItemResponse>>
{
    private readonly IInvitacionEquipoRepository _invitacionRepository;
    private readonly IEquipoRepository _equipoRepository;

    public GetInvitacionesRecibidasQueryHandler(
        IInvitacionEquipoRepository invitacionRepository,
        IEquipoRepository equipoRepository)
    {
        _invitacionRepository = invitacionRepository;
        _equipoRepository = equipoRepository;
    }

    public async Task<IReadOnlyList<InvitacionRecibidasItemResponse>> Handle(GetInvitacionesRecibidasQuery request, CancellationToken cancellationToken)
    {
        var invitaciones = await _invitacionRepository.GetPendientesByInvitadoAsync(request.ActorUserId, cancellationToken);

        var result = new List<InvitacionRecibidasItemResponse>(invitaciones.Count);

        foreach (var inv in invitaciones)
        {
            var equipo = await _equipoRepository.GetByIdAsync(inv.EquipoId, cancellationToken);
            var nombreEquipo = equipo?.NombreEquipo ?? string.Empty;

            result.Add(new InvitacionRecibidasItemResponse(
                inv.InvitacionEquipoId,
                inv.EquipoId,
                nombreEquipo,
                inv.InvitadoPorSubjectId,
                inv.FechaCreacionUtc));
        }

        return result;
    }
}
