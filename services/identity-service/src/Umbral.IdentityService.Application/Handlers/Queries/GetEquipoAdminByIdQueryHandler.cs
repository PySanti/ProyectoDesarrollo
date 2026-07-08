using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetEquipoAdminByIdQueryHandler : IRequestHandler<GetEquipoAdminByIdQuery, EquipoAdminResponse?>
{
    private readonly IEquipoRepository _equipos;

    public GetEquipoAdminByIdQueryHandler(IEquipoRepository equipos) => _equipos = equipos;

    public async Task<EquipoAdminResponse?> Handle(GetEquipoAdminByIdQuery request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetByIdAsync(request.EquipoId, cancellationToken);
        return equipo is null ? null : GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
