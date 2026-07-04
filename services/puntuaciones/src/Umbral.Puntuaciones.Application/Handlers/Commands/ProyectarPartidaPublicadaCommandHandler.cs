using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarPartidaPublicadaCommandHandler : IRequestHandler<ProyectarPartidaPublicadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarPartidaPublicadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarPartidaPublicadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            _repo.AddPartida(PartidaProyectada.DesdePublicacion(request.PartidaId, request.SesionPartidaId, request.Modalidad));
        }
        else
        {
            partida.RegistrarPublicacion(request.Modalidad);
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PartidaPublicadaEnLobby", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
