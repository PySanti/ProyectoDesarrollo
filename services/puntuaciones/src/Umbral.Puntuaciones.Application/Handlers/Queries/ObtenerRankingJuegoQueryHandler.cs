using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public sealed class ObtenerRankingJuegoQueryHandler : IRequestHandler<ObtenerRankingJuegoQuery, RankingJuegoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRankingJuegoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RankingJuegoResponse> Handle(ObtenerRankingJuegoQuery request, CancellationToken cancellationToken)
    {
        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null || juego.PartidaId != request.PartidaId)
        {
            throw new JuegoNoEncontradoException(request.JuegoId);
        }

        var marcadores = await _repo.GetMarcadoresDeJuegoAsync(request.JuegoId, cancellationToken);
        return new RankingJuegoResponse(juego.JuegoId, juego.TipoJuego, DateTime.UtcNow, RankingCalculator.Calcular(marcadores));
    }
}
