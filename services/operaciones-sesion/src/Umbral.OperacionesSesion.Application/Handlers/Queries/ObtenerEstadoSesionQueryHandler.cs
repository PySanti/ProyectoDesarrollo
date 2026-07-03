using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerEstadoSesionQueryHandler : IRequestHandler<ObtenerEstadoSesionQuery, EstadoSesionDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerEstadoSesionQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<EstadoSesionDto> Handle(ObtenerEstadoSesionQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juegos = sesion.Juegos
            .OrderBy(j => j.Orden)
            .Select(j => new JuegoEstadoDto(j.JuegoId, j.Orden, j.TipoJuego.ToString(), j.Estado.ToString()))
            .ToList();

        var actual = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);

        return new EstadoSesionDto(
            sesion.PartidaId,
            sesion.Id.Valor,
            sesion.Estado.ToString(),
            sesion.Modalidad.ToString(),
            juegos,
            actual?.Orden);
    }
}
