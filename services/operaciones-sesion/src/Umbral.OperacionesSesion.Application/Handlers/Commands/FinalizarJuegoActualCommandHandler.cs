using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class FinalizarJuegoActualCommandHandler : IRequestHandler<FinalizarJuegoActualCommand, AvanceJuegoResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public FinalizarJuegoActualCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<AvanceJuegoResponse> Handle(FinalizarJuegoActualCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.FinalizarJuegoActual(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (resultado.Tipo == TipoResultadoAvance.Avanzado)
        {
            var juego = resultado.JuegoActivado!;
            await _events.PublicarJuegoActivadoAsync(
                new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()),
                cancellationToken);
            await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(_events, sesion, juego, cancellationToken);
            await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, cancellationToken);
        }
        else
        {
            await _events.PublicarPartidaFinalizadaAsync(
                new PartidaFinalizadaEvent(sesion.PartidaId, sesion.Id.Valor, now),
                cancellationToken);
        }

        return new AvanceJuegoResponse(
            sesion.PartidaId,
            sesion.Estado.ToString(),
            resultado.JuegoFinalizado.Orden,
            resultado.JuegoActivado?.Orden,
            resultado.Terminada());
    }
}
