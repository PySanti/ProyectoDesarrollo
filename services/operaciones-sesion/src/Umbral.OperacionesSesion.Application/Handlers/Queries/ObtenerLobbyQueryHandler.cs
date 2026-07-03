using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerLobbyQueryHandler : IRequestHandler<ObtenerLobbyQuery, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerLobbyQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<LobbyDto> Handle(ObtenerLobbyQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);
        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
