using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetHistorialNombresEquipoQueryHandler
    : IRequestHandler<GetHistorialNombresEquipoQuery, HistorialNombresEquipoResponse>
{
    private readonly IHistorialNombreEquipoRepository _historial;

    public GetHistorialNombresEquipoQueryHandler(IHistorialNombreEquipoRepository historial)
        => _historial = historial;

    public async Task<HistorialNombresEquipoResponse> Handle(
        GetHistorialNombresEquipoQuery request, CancellationToken cancellationToken)
    {
        var registros = await _historial.GetByUsuarioAsync(request.ActorUserId, cancellationToken);
        var items = registros
            .Select(r => new HistorialNombreEquipoItem(r.NombreEquipo, r.EquipoId, r.FechaRegistroUtc))
            .ToList();
        return new HistorialNombresEquipoResponse(items);
    }
}
