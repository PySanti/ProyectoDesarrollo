using MediatR;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class BarrerTimeoutsCommandHandler : IRequestHandler<BarrerTimeoutsCommand, int>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BarrerTimeoutsCommandHandler> _logger;

    public BarrerTimeoutsCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider,
        ILogger<BarrerTimeoutsCommandHandler> logger)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(BarrerTimeoutsCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidatos = await _sesiones.GetSesionesConActividadVencidaAsync(now, cancellationToken);
        var avanzadas = 0;

        foreach (var sesion in candidatos)
        {
            try
            {
                var r = sesion.CerrarActividadVencida(now);
                if (!r.HuboCambio) continue;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await PublicarAsync(sesion, r, now, cancellationToken);
                avanzadas++;
            }
            catch (OperationCanceledException)
            {
                throw; // shutdown del host: no tragar la cancelación, abortar el barrido
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrido de timeout: candidato {PartidaId} saltado.", sesion.PartidaId);
            }
        }

        return avanzadas;
    }

    private async Task PublicarAsync(SesionPartida sesion, ResultadoCierreVencido r, DateTime now, CancellationToken ct)
    {
        if (r.Tipo == TipoCierreVencido.Trivia)
        {
            var p = r.Pregunta!;
            var juegoCerrado = sesion.Juegos.First(j => j.JuegoId == p.JuegoId);
            var preguntaCerrada = juegoCerrado.Preguntas.First(preg => preg.PreguntaId == p.PreguntaCerradaId);
            var opcionCorrectaCerrada = preguntaCerrada.Opciones.First(o => o.EsCorrecta);
            await _events.PublicarPreguntaTriviaCerradaAsync(
                new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, p.JuegoId, p.PreguntaCerradaId,
                    p.MotivoCierre.ToString(), now, null, null,
                    opcionCorrectaCerrada.OpcionId, opcionCorrectaCerrada.Texto), ct);
            if (p.PreguntaActivadaId is not null)
                await _events.PublicarPreguntaTriviaActivadaAsync(
                    new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, p.JuegoId, p.PreguntaActivadaId.Value,
                        p.PreguntaActivadaOrden!.Value, p.TiempoLimiteActivadaSegundos!.Value, p.FechaActivacionActivada!.Value), ct);
        }
        else if (r.Tipo == TipoCierreVencido.Bdt)
        {
            var e = r.Etapa!;
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(sesion.PartidaId, sesion.Id.Valor, e.JuegoId, e.EtapaCerradaId,
                    e.MotivoCierre.ToString(), now, null), ct);
            if (e.EtapaActivadaId is not null)
                await _events.PublicarEtapaBDTActivadaAsync(
                    new EtapaBDTActivadaEvent(sesion.PartidaId, sesion.Id.Valor, e.JuegoId, e.EtapaActivadaId.Value,
                        e.EtapaActivadaOrden!.Value, e.TiempoLimiteActivadaSegundos!.Value, e.FechaActivacionActivada!.Value), ct);
        }

        if (r.JuegoFinalizado is { } fin)
        {
            if (fin.Tipo == TipoResultadoAvance.Avanzado)
            {
                var juego = fin.JuegoActivado!;
                await _events.PublicarJuegoActivadoAsync(
                    new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()), ct);
                await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(_events, sesion, juego, ct);
                await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, ct);
            }
            else
            {
                await _events.PublicarPartidaFinalizadaAsync(
                    new PartidaFinalizadaEvent(sesion.PartidaId, sesion.Id.Valor, now), ct);
            }
        }
    }
}
