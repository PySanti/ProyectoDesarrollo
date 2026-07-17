using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetEquiposAdminQueryHandler : IRequestHandler<GetEquiposAdminQuery, IReadOnlyList<EquipoAdminResponse>>
{
    private readonly IEquipoRepository _equipos;

    public GetEquiposAdminQueryHandler(IEquipoRepository equipos) => _equipos = equipos;

    public async Task<IReadOnlyList<EquipoAdminResponse>> Handle(GetEquiposAdminQuery request, CancellationToken cancellationToken)
    {
        var equipos = await _equipos.GetAllAsync(cancellationToken);
        return equipos.Select(MapToResponse).ToList();
    }

    internal static EquipoAdminResponse MapToResponse(Equipo equipo)
    {
        return new EquipoAdminResponse(
            equipo.EquipoId,
            equipo.NombreEquipo,
            equipo.Estado.ToString(),
            equipo.Participantes.FirstOrDefault(p => p.EsLider)?.SubjectId,
            equipo.Participantes
                .Select(p => new EquipoAdminIntegrante(p.SubjectId, p.EsLider))
                .ToList());
    }
}
