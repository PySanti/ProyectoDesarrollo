using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ValidarTesoroCommandHandler : IRequestHandler<ValidarTesoroCommand, ValidacionTesoroResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly IQrDecoder _decoder;

    public ValidarTesoroCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider,
        IQrDecoder decoder)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _decoder = decoder;
    }

    public async Task<ValidacionTesoroResponse> Handle(ValidarTesoroCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var imagen = Convert.FromBase64String(request.ImagenBase64);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.ValidarTesoro(request.ParticipanteId, imagen, now, _decoder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Siempre: TesoroQRValidado (tras SAVE)
        await _events.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(
                sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                r.ParticipanteId, r.Resultado.ToString(), r.Instante, r.EquipoId),
            cancellationToken);

        if (r.Gano)
        {
            await _events.PublicarEtapaBDTGanadaAsync(
                new EtapaBDTGanadaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    r.ParticipanteId, r.Puntaje!.Value, r.TiempoResolucionMs!.Value, r.EquipoId),
                cancellationToken);
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    "Ganador", now, r.GanadorParticipanteId, r.GanadorEquipoId),
                cancellationToken);
            var juego = sesion.Juegos.First(j => j.JuegoId == r.JuegoId);
            await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, cancellationToken);
        }
        else if (r.CerroEtapa)
        {
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    "Tiempo", now, null),
                cancellationToken);
            var juego = sesion.Juegos.First(j => j.JuegoId == r.JuegoId);
            await IniciarPartidaCommandHandler.PublicarEtapaActivadaSiBdtAsync(_events, sesion, juego, cancellationToken);
        }

        // Si ganar/cerrar cerró la última etapa, el juego se finalizó: avanzar o terminar.
        await IniciarPartidaCommandHandler.PublicarFinDeJuegoAsync(_events, sesion, r.JuegoFinalizado, now, cancellationToken);

        return new ValidacionTesoroResponse(
            sesion.PartidaId, r.EtapaId, r.Resultado.ToString(), r.Gano, r.CerroEtapa, r.Puntaje);
    }
}
