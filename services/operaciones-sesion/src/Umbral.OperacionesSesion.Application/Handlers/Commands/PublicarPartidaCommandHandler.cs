using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class PublicarPartidaCommandHandler : IRequestHandler<PublicarPartidaCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly IConfiguracionPartidaClient _configClient;
    private readonly ISesionEventsPublisher _events;

    public PublicarPartidaCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        IConfiguracionPartidaClient configClient,
        ISesionEventsPublisher events)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _configClient = configClient;
        _events = events;
    }

    public async Task<LobbyDto> Handle(PublicarPartidaCommand request, CancellationToken cancellationToken)
    {
        if (await _sesiones.ExistsForPartidaAsync(request.PartidaId, cancellationToken))
            throw new SesionYaPublicadaException(request.PartidaId);

        var config = await _configClient.ObtenerConfiguracionAsync(request.PartidaId, request.BearerToken, cancellationToken)
            ?? throw new PartidaConfigNoEncontradaException(request.PartidaId);

        var snapshot = new ConfiguracionSnapshot(
            config.Nombre,
            Enum.Parse<Modalidad>(config.Modalidad, ignoreCase: true),
            Enum.Parse<ModoInicioPartida>(config.ModoInicioPartida, ignoreCase: true),
            config.TiempoInicio,
            config.MinimosParticipacion,
            config.MaximosParticipacion,
            config.Juegos.Select(MapearJuego).ToList());

        var sesion = SesionPartida.Publicar(request.PartidaId, snapshot);
        _sesiones.Add(sesion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarPartidaPublicadaEnLobbyAsync(
            new PartidaPublicadaEnLobbyEvent(
                sesion.PartidaId, sesion.Id.Valor, sesion.Modalidad.ToString(),
                sesion.MinimosParticipacion, sesion.MaximosParticipacion),
            cancellationToken);

        return MapearLobby(sesion);
    }

    internal static LobbyDto MapearLobby(SesionPartida sesion)
    {
        var activas = sesion.Inscripciones.Where(i => i.EsActiva).ToList();
        return new LobbyDto(
            sesion.PartidaId,
            sesion.Id.Valor,
            sesion.Estado.ToString(),
            sesion.Modalidad.ToString(),
            sesion.MinimosParticipacion,
            sesion.MaximosParticipacion,
            activas.Count,
            activas.Where(i => i.Modalidad == Modalidad.Individual).Select(i => i.ParticipanteId).ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Equipo && i.EsActiva && i.EquipoId is not null)
                .Select(i => new EquipoLobbyDto(i.EquipoId!.Value, i.Convocatorias.Count, i.ConvocatoriasAceptadas))
                .ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Individual && i.EstaPendiente)
                .Select(i => new SolicitudIndividualDto(i.Id.Valor, i.ParticipanteId, i.FechaInscripcion))
                .ToList(),
            sesion.Inscripciones
                .Where(i => i.Modalidad == Modalidad.Equipo && i.EstaPendiente && i.EquipoId is not null)
                .Select(i => new SolicitudEquipoDto(i.Id.Valor, i.EquipoId!.Value, i.MiembrosSnapshot.Count, i.FechaInscripcion))
                .ToList());
    }

    private static JuegoResumen MapearJuego(JuegoResumenDto j)
    {
        var tipo = Enum.Parse<TipoJuego>(j.TipoJuego, ignoreCase: true);

        if (tipo == TipoJuego.Trivia && j.Trivia is not null)
        {
            var preguntas = j.Trivia.Preguntas
                .Select((p, idx) => new PreguntaSnapshot(
                    p.PreguntaId, idx + 1, p.Texto, p.PuntajeAsignado, p.TiempoLimiteSegundos,
                    p.Opciones.Select(o => new OpcionSnapshot(o.OpcionId, o.Texto, o.EsCorrecta)).ToList()))
                .ToList();
            return new JuegoResumen(j.JuegoId, j.Orden, tipo, preguntas);
        }

        if (tipo == TipoJuego.BusquedaDelTesoro && j.Bdt is not null)
        {
            var etapas = j.Bdt.Etapas
                .Select((e, idx) => new EtapaSnapshot(
                    e.EtapaBDTId, idx + 1, e.CodigoQREsperado, e.PuntajeAsignado, e.TiempoLimiteSegundos))
                .ToList();
            return new JuegoResumen(j.JuegoId, j.Orden, tipo, j.Bdt.AreaBusqueda, etapas);
        }

        return new JuegoResumen(j.JuegoId, j.Orden, tipo); // sin contenido
    }
}
