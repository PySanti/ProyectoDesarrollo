using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerMiSesionQueryHandler : IRequestHandler<ObtenerMiSesionQuery, MiSesionDto?>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerMiSesionQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<MiSesionDto?> Handle(ObtenerMiSesionQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByParticipanteActivoAsync(request.ParticipanteId, cancellationToken);
        if (sesion is null) return null;

        var inscripcion = sesion.Inscripciones.FirstOrDefault(
            i => i.EsActiva && i.ParticipanteId == request.ParticipanteId);

        var convocatoria = sesion.Inscripciones
            .Where(i => i.EsActiva)
            .SelectMany(i => i.Convocatorias)
            .FirstOrDefault(c => c.UsuarioId == request.ParticipanteId);

        var inscId = inscripcion?.Id.Valor ?? Guid.Empty;
        var inscEstado = inscripcion?.Estado.ToString() ?? "Equipo";
        var inscDto = new InscripcionResumenDto(inscId, inscEstado);

        MiConvocatoriaDto? convocatoriaDto = convocatoria is null
            ? null
            : new MiConvocatoriaDto(convocatoria.Id.Valor, convocatoria.EquipoId, convocatoria.Estado.ToString());

        JuegoActivoResumenDto? juegoDto = null;
        PreguntaActualDto? preguntaDto = null;
        EtapaActualDto? etapaDto = null;
        bool? yaRespondio = null;

        if (sesion.Estado == EstadoSesion.Iniciada)
        {
            var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
            if (juego is not null)
            {
                juegoDto = new JuegoActivoResumenDto(
                    juego.JuegoId, juego.Orden, juego.TipoJuego.ToString(), juego.Estado.ToString());

                if (juego.TipoJuego == TipoJuego.Trivia && juego.PreguntaActiva is { } preg)
                {
                    preguntaDto = new PreguntaActualDto(
                        sesion.PartidaId, juego.JuegoId, preg.PreguntaId, preg.Orden, preg.Texto,
                        preg.TiempoLimiteSegundos, preg.FechaActivacion!.Value,
                        preg.Opciones.Select(o => new OpcionPublicaDto(o.OpcionId, o.Texto)).ToList());
                    yaRespondio = sesion.Modalidad == Modalidad.Equipo && convocatoria is not null
                        ? preg.Respuestas.Any(r => r.EquipoId == convocatoria.EquipoId)
                        : preg.Respuestas.Any(r => r.ParticipanteId == request.ParticipanteId);
                }
                else if (juego.TipoJuego == TipoJuego.BusquedaDelTesoro && juego.EtapaActiva is { } et)
                {
                    etapaDto = new EtapaActualDto(
                        sesion.PartidaId, juego.JuegoId, et.EtapaId, et.Orden, juego.AreaBusqueda,
                        et.TiempoLimiteSegundos, et.FechaActivacion!.Value);
                }
            }
        }

        return new MiSesionDto(
            sesion.PartidaId, sesion.Id.Valor, sesion.Estado.ToString(), sesion.Modalidad.ToString(),
            inscDto, juegoDto, preguntaDto, etapaDto, yaRespondio, convocatoriaDto);
    }
}
