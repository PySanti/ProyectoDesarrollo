using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerPreguntaActualQueryHandler : IRequestHandler<ObtenerPreguntaActualQuery, PreguntaActualDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerPreguntaActualQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<PreguntaActualDto> Handle(ObtenerPreguntaActualQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        var pregunta = juego?.PreguntaActiva ?? throw new NoHayPreguntaActivaException(request.PartidaId);

        return new PreguntaActualDto(
            sesion.PartidaId, juego!.JuegoId, pregunta.PreguntaId, pregunta.Orden, pregunta.Texto,
            pregunta.TiempoLimiteSegundos, pregunta.FechaActivacion!.Value,
            pregunta.Opciones.Select(o => new OpcionPublicaDto(o.OpcionId, o.Texto)).ToList());
    }
}
