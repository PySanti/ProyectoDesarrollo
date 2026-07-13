using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

// HU-38: monitoreo operador de los envíos de TesoroQR del juego BDT activo, por etapa.
public sealed class ObtenerEnviosTesoroQueryHandler : IRequestHandler<ObtenerEnviosTesoroQuery, EnviosTesoroDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerEnviosTesoroQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<EnviosTesoroDto> Handle(ObtenerEnviosTesoroQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo)
            ?? throw new NoHayEtapaActivaException(request.PartidaId);
        if (juego.TipoJuego != TipoJuego.BusquedaDelTesoro)
            throw new JuegoActivoNoEsBDTException(request.PartidaId);

        var etapas = juego.Etapas
            .OrderBy(e => e.Orden)
            .Select(e => new EtapaEnviosDto(
                e.EtapaId, e.Orden,
                e.Tesoros
                    .Select(t => new IntentoTesoroDto(t.ParticipanteId, t.EquipoId, t.Resultado.ToString(), t.FechaEnvio))
                    .ToList()))
            .ToList();

        return new EnviosTesoroDto(sesion.PartidaId, juego.JuegoId, etapas);
    }
}
