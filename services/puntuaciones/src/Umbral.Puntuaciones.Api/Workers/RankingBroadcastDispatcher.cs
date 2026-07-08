using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Interfaces;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Workers;

// Difusión best-effort tras una proyección exitosa (SP-4c): resuelve el ranking recalculado con la
// misma query que sirve el GET y lo publica por SignalR. Cualquier fallo se degrada a warning:
// la proyección y el ack del worker nunca dependen del push (ADR-0012).
public sealed class RankingBroadcastDispatcher
{
    private readonly ISender _sender;
    private readonly IRankingRealtimePublisher _publisher;
    private readonly ILogger<RankingBroadcastDispatcher> _logger;

    public RankingBroadcastDispatcher(ISender sender, IRankingRealtimePublisher publisher,
        ILogger<RankingBroadcastDispatcher> logger)
    {
        _sender = sender;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DifundirAsync(object comandoProyectado, CancellationToken ct)
    {
        try
        {
            switch (comandoProyectado)
            {
                case ProyectarPuntajeTriviaCommand c:
                    await _publisher.PublicarRankingTriviaActualizadoAsync(c.PartidaId,
                        await _sender.Send(new ObtenerRankingJuegoQuery(c.PartidaId, c.JuegoId), ct), ct);
                    break;
                case ProyectarEtapaBdtGanadaCommand c:
                    await _publisher.PublicarRankingBdtActualizadoAsync(c.PartidaId,
                        await _sender.Send(new ObtenerRankingJuegoQuery(c.PartidaId, c.JuegoId), ct), ct);
                    break;
                case ProyectarPartidaFinalizadaCommand c:
                    await _publisher.PublicarRankingConsolidadoCalculadoAsync(
                        await _sender.Send(new ObtenerRankingConsolidadoQuery(c.PartidaId), ct), ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Fallo difundiendo ranking tras proyectar {Comando}; la proyección y el ack no se ven afectados.",
                comandoProyectado.GetType().Name);
        }
    }
}
