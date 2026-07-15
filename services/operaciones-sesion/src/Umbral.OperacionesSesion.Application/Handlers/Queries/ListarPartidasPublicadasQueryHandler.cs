using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ListarPartidasPublicadasQueryHandler
    : IRequestHandler<ListarPartidasPublicadasQuery, IReadOnlyList<PartidaPublicadaDto>>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ListarPartidasPublicadasQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<IReadOnlyList<PartidaPublicadaDto>> Handle(
        ListarPartidasPublicadasQuery request, CancellationToken cancellationToken)
    {
        var sesiones = await _sesiones.GetSesionesEnLobbyAsync(cancellationToken);
        return sesiones
            .Select(s => new PartidaPublicadaDto(
                s.PartidaId,
                s.Nombre,
                s.Modalidad.ToString(),
                s.ModoInicioPartida.ToString(),
                s.TiempoInicio,
                s.MinimosParticipacion,
                s.MaximosParticipacion,
                s.Inscripciones.Count(i => i.EsActiva)))
            .ToList();
    }
}
