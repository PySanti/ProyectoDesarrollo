using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// RF-44: posición en el consolidado y si la ganó, por partida por equipos terminada donde el
// equipo anotó. "Sin duplicar el cálculo de puntajes": reusa CalculadorRankingConsolidado.
public sealed class ObtenerRendimientoEquipoQueryHandler
    : IRequestHandler<ObtenerRendimientoEquipoQuery, RendimientoEquipoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRendimientoEquipoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RendimientoEquipoResponse> Handle(ObtenerRendimientoEquipoQuery request, CancellationToken cancellationToken)
    {
        var partidas = await _repo.GetPartidasTerminadasConMarcadorDeEquipoAsync(request.EquipoId, cancellationToken);
        var rendimiento = new List<RendimientoPartidaDto>(partidas.Count);
        foreach (var partida in partidas)
        {
            var marcadores = await _repo.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var entradas = CalculadorRankingConsolidado.Calcular(marcadores);
            // El repo garantiza ≥1 marcador del equipo en cada partida devuelta.
            var propia = entradas.First(e => e.CompetidorId == request.EquipoId);
            rendimiento.Add(new RendimientoPartidaDto(partida.PartidaId, partida.FechaFin, propia.Posicion, propia.Posicion == 1));
        }

        return new RendimientoEquipoResponse(request.EquipoId, rendimiento);
    }
}
