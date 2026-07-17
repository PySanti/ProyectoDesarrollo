using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class ObtenerMiEquipoQueryHandler : IRequestHandler<ObtenerMiEquipoQuery, EquipoMineResponse?>
{
    private readonly IEquipoRepository _equipos;

    public ObtenerMiEquipoQueryHandler(IEquipoRepository equipos) => _equipos = equipos;

    public async Task<EquipoMineResponse?> Handle(ObtenerMiEquipoQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);
        if (equipo is null) return null;

        return new EquipoMineResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            equipo.Participantes
                .Select(p => new MiembroEquipoResponse(p.SubjectId, p.EsLider))
                .ToList());
    }
}
