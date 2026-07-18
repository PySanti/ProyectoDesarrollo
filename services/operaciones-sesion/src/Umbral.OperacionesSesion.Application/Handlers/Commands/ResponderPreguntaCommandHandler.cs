using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ResponderPreguntaCommandHandler : IRequestHandler<ResponderPreguntaCommand, RespuestaTriviaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public ResponderPreguntaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<RespuestaTriviaResponse> Handle(ResponderPreguntaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.ResponderPregunta(request.ParticipanteId, request.OpcionId, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                r.ParticipanteId, r.OpcionId, r.EsCorrecta, r.Instante, r.EquipoId), cancellationToken);

        if (r.CerroPregunta)
        {
            var juego = sesion.Juegos.First(j => j.JuegoId == r.JuegoId);
            var pregunta = juego.Preguntas.First(p => p.PreguntaId == r.PreguntaId);
            var opcionCorrecta = pregunta.Opciones.First(o => o.EsCorrecta);

            // Dos formas de cierre en respuesta: por acierto (hay ganador y puntaje) o porque todos
            // respondieron mal (sin ganador ni puntaje). Ambas revelan la correcta y avanzan.
            if (r.EsCorrecta)
                await _events.PublicarPuntajeTriviaIncrementadoAsync(
                    new PuntajeTriviaIncrementadoEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                        r.ParticipanteId, r.Puntaje!.Value, r.TiempoRespuestaMs, r.EquipoId), cancellationToken);

            var motivo = r.EsCorrecta ? MotivoCierrePregunta.RespuestaCorrecta : MotivoCierrePregunta.TodosRespondieron;
            await _events.PublicarPreguntaTriviaCerradaAsync(
                new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                    motivo.ToString(), r.Instante,
                    r.EsCorrecta ? r.ParticipanteId : null,
                    r.EsCorrecta ? r.EquipoId : null,
                    opcionCorrecta.OpcionId, opcionCorrecta.Texto), cancellationToken);
            await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(_events, sesion, juego, cancellationToken);
        }

        // Si el acierto cerró la última pregunta, el juego se finalizó: avanzar o terminar.
        await IniciarPartidaCommandHandler.PublicarFinDeJuegoAsync(_events, sesion, r.JuegoFinalizado, now, cancellationToken);

        return new RespuestaTriviaResponse(sesion.PartidaId, r.PreguntaId, r.EsCorrecta, r.CerroPregunta, r.Puntaje);
    }
}
