using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerEtapaActualQueryHandler : IRequestHandler<ObtenerEtapaActualQuery, EtapaActualDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerEtapaActualQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<EtapaActualDto> Handle(ObtenerEtapaActualQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        var etapa = juego?.EtapaActiva ?? throw new NoHayEtapaActivaException(request.PartidaId);

        return new EtapaActualDto(
            sesion.PartidaId, juego!.JuegoId, etapa.EtapaId, etapa.Orden, juego.AreaBusqueda,
            etapa.TiempoLimiteSegundos, etapa.FechaActivacion!.Value);
    }
}
