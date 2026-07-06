using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerRankingConsolidadoQueryHandler
    : IRequestHandler<ObtenerRankingConsolidadoQuery, RankingConsolidadoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRankingConsolidadoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RankingConsolidadoResponse> Handle(ObtenerRankingConsolidadoQuery request, CancellationToken cancellationToken)
    {
        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            throw new PartidaNoEncontradaException(request.PartidaId);
        }
        if (partida.Estado != EstadoPartidaProyectada.Terminada)
        {
            throw new PartidaNoTerminadaException(request.PartidaId, partida.Estado);
        }

        var marcadores = await _repo.GetMarcadoresDePartidaAsync(request.PartidaId, cancellationToken);
        return new RankingConsolidadoResponse(
            request.PartidaId, DateTime.UtcNow, CalculadorRankingConsolidado.Calcular(marcadores));
    }
}
