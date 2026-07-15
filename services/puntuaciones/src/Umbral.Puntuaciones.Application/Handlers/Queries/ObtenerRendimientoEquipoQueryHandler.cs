using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// RF-44: posición en el consolidado y si la ganó, por partida por equipos terminada donde el equipo
// participó (inscripción aceptada), anotara o no. "Sin duplicar el cálculo de puntajes": reusa
// CalculadorRankingConsolidado.
public sealed class ObtenerRendimientoEquipoQueryHandler
    : IRequestHandler<ObtenerRendimientoEquipoQuery, RendimientoEquipoResponse>
{
    private readonly IProyeccionesRepository _repo;

    public ObtenerRendimientoEquipoQueryHandler(IProyeccionesRepository repo) => _repo = repo;

    public async Task<RendimientoEquipoResponse> Handle(ObtenerRendimientoEquipoQuery request, CancellationToken cancellationToken)
    {
        var partidas = await _repo.GetPartidasTerminadasConParticipacionDeEquipoAsync(request.EquipoId, cancellationToken);
        var rendimiento = new List<RendimientoPartidaDto>(partidas.Count);
        foreach (var partida in partidas)
        {
            var marcadores = await _repo.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            var participaciones = await _repo.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
            var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
            // El repo garantiza participación o marcador del equipo en cada partida devuelta, y
            // ambos están en el universo del calculador: la entrada existe.
            var propia = entradas.First(e => e.CompetidorId == request.EquipoId);
            rendimiento.Add(new RendimientoPartidaDto(partida.PartidaId, partida.FechaFin, propia.Posicion, propia.Posicion == 1));
        }

        return new RendimientoEquipoResponse(request.EquipoId, rendimiento);
    }
}
