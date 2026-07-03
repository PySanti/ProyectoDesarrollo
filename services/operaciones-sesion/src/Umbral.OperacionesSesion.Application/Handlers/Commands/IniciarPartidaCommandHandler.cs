using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class IniciarPartidaCommandHandler : IRequestHandler<IniciarPartidaCommand, InicioPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public IniciarPartidaCommandHandler(
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

    public async Task<InicioPartidaResponse> Handle(IniciarPartidaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.Iniciar(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
        return MapearInicio(sesion, resultado);
    }

    internal static async Task PublicarEventosInicioAsync(
        ISesionEventsPublisher events, SesionPartida sesion, ResultadoInicio resultado, DateTime now, CancellationToken cancellationToken)
    {
        switch (resultado.Tipo)
        {
            case TipoResultadoInicio.Iniciada:
                var juego = resultado.JuegoActivado!;
                await events.PublicarPartidaIniciadaAsync(
                    new PartidaIniciadaEvent(sesion.PartidaId, sesion.Id.Valor, now, juego.JuegoId, juego.Orden),
                    cancellationToken);
                await events.PublicarJuegoActivadoAsync(
                    new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()),
                    cancellationToken);
                await PublicarPreguntaActivadaSiTriviaAsync(events, sesion, juego, cancellationToken);
                await PublicarEtapaActivadaSiBdtAsync(events, sesion, juego, cancellationToken);
                break;
            case TipoResultadoInicio.Cancelada:
                await events.PublicarPartidaCanceladaAsync(
                    new PartidaCanceladaEvent(sesion.PartidaId, sesion.Id.Valor, "MinimosNoAlcanzados", now),
                    cancellationToken);
                break;
            // NoCorresponde → no event
        }
    }

    internal static async Task PublicarPreguntaActivadaSiTriviaAsync(
        ISesionEventsPublisher events, SesionPartida sesion, JuegoResumen juego, CancellationToken cancellationToken)
    {
        var pregunta = juego.PreguntaActiva;
        if (pregunta is null) return;
        await events.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, pregunta.PreguntaId,
                pregunta.Orden, pregunta.TiempoLimiteSegundos, pregunta.FechaActivacion!.Value),
            cancellationToken);
    }

    internal static async Task PublicarEtapaActivadaSiBdtAsync(
        ISesionEventsPublisher events, SesionPartida sesion, JuegoResumen juego, CancellationToken cancellationToken)
    {
        var etapa = juego.EtapaActiva;
        if (etapa is null) return;
        await events.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(
                sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, etapa.EtapaId,
                etapa.Orden, etapa.TiempoLimiteSegundos, etapa.FechaActivacion!.Value),
            cancellationToken);
    }

    internal static InicioPartidaResponse MapearInicio(SesionPartida sesion, ResultadoInicio resultado) =>
        new(sesion.PartidaId, sesion.Estado.ToString(), resultado.JuegoActivado?.JuegoId, resultado.JuegoActivado?.Orden);
}
