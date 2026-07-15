using MediatR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// HU-27 (RF-24): historial único de partidas jugadas con puntuación y posición. Participación:
// (a) marcador propio en Individual; (b) equipo resuelto del historial (acciones de juego
// autoradas, sin ConvocatoriaCreada) con ≥1 marcador del equipo. Posición/gano del mismo
// CalculadorRankingConsolidado de SP-4b (RF-44: sin duplicar el cálculo). Limitación documentada:
// el integrante que jamás autoró una acción de juego no ve la partida; canceladas excluidas (RB-30).
public sealed class ObtenerHistorialPartidasQueryHandler
    : IRequestHandler<ObtenerHistorialPartidasQuery, HistorialPartidasResponse>
{
    private readonly IProyeccionesRepository _proyecciones;
    private readonly IHistorialRepository _historial;

    public ObtenerHistorialPartidasQueryHandler(IProyeccionesRepository proyecciones, IHistorialRepository historial)
    {
        _proyecciones = proyecciones;
        _historial = historial;
    }

    public async Task<HistorialPartidasResponse> Handle(
        ObtenerHistorialPartidasQuery request, CancellationToken cancellationToken)
    {
        var partidas = new List<PartidaJugadaDto>();

        var individuales = await _proyecciones.GetPartidasTerminadasConMarcadorDeParticipanteAsync(
            request.ParticipanteId, cancellationToken);
        foreach (var partida in individuales)
        {
            partidas.Add(await ConstruirPartidaJugadaAsync(
                partida, competidorId: request.ParticipanteId, equipoId: null, cancellationToken));
        }

        foreach (var participacion in await _historial.GetEquiposDelParticipanteAsync(request.ParticipanteId, cancellationToken))
        {
            var partida = await _proyecciones.GetPartidaAsync(participacion.PartidaId, cancellationToken);
            if (partida is null || partida.Estado != EstadoPartidaProyectada.Terminada)
            {
                continue;
            }
            var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
            if (!marcadores.Any(m => m.CompetidorId == participacion.EquipoId && m.TipoCompetidor == TipoCompetidor.Equipo))
            {
                continue;
            }
            partidas.Add(await ConstruirPartidaJugadaAsync(
                partida, competidorId: participacion.EquipoId, equipoId: participacion.EquipoId, cancellationToken));
        }

        return new HistorialPartidasResponse(
            request.ParticipanteId,
            partidas.OrderByDescending(p => p.FechaFin).ToList());
    }

    private async Task<PartidaJugadaDto> ConstruirPartidaJugadaAsync(
        PartidaProyectada partida, Guid competidorId, Guid? equipoId, CancellationToken cancellationToken)
    {
        var marcadores = await _proyecciones.GetMarcadoresDePartidaAsync(partida.PartidaId, cancellationToken);
        var participaciones = await _proyecciones.GetParticipacionesDePartidaAsync(partida.PartidaId, cancellationToken);
        var entradas = CalculadorRankingConsolidado.Calcular(marcadores, participaciones);
        // La participación exige ≥1 marcador del competidor, así que la entrada siempre existe.
        var propia = entradas.First(e => e.CompetidorId == competidorId);

        var juegos = (await _proyecciones.GetJuegosDePartidaAsync(partida.PartidaId, cancellationToken))
            .Select(j => new JuegoJugadoDto(
                j.JuegoId, j.Orden, j.TipoJuego,
                marcadores.FirstOrDefault(m => m.JuegoId == j.JuegoId && m.CompetidorId == competidorId)
                    ?.PuntosAcumulados ?? 0))
            .ToList();

        return new PartidaJugadaDto(
            partida.PartidaId, partida.Modalidad, partida.FechaFin, equipoId,
            propia.PuntosTotales, propia.Posicion, propia.Posicion == 1, juegos);
    }
}
