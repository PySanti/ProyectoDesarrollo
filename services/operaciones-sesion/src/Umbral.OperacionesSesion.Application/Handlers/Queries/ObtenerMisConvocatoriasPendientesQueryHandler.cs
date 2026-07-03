using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerMisConvocatoriasPendientesQueryHandler
    : IRequestHandler<ObtenerMisConvocatoriasPendientesQuery, IReadOnlyList<ConvocatoriaPendienteDto>>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerMisConvocatoriasPendientesQueryHandler(ISesionPartidaRepository sesiones)
        => _sesiones = sesiones;

    public async Task<IReadOnlyList<ConvocatoriaPendienteDto>> Handle(
        ObtenerMisConvocatoriasPendientesQuery request, CancellationToken cancellationToken)
    {
        var pendientes = await _sesiones.GetConvocatoriasPendientesByUsuarioAsync(request.UsuarioId, cancellationToken);
        return pendientes
            .Select(c => new ConvocatoriaPendienteDto(c.Id.Valor, c.PartidaId, c.EquipoId, c.FechaEnvio))
            .ToList();
    }
}
