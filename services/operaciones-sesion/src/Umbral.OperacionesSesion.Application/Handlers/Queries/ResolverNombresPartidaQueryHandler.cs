using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ResolverNombresPartidaQueryHandler
    : IRequestHandler<ResolverNombresPartidaQuery, ResolverNombresPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ResolverNombresPartidaQueryHandler(ISesionPartidaRepository sesiones)
        => _sesiones = sesiones;

    public async Task<ResolverNombresPartidaResponse> Handle(
        ResolverNombresPartidaQuery request, CancellationToken cancellationToken)
    {
        if (request.PartidaIds.Count == 0)
            return new ResolverNombresPartidaResponse(Array.Empty<NombrePartidaDto>());

        var nombres = await _sesiones.GetNombresByPartidaIdsAsync(request.PartidaIds, cancellationToken);
        return new ResolverNombresPartidaResponse(
            nombres.Select(n => new NombrePartidaDto(n.PartidaId, n.Nombre)).ToList());
    }
}
