using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarPartidaCanceladaCommandHandler : IRequestHandler<ProyectarPartidaCanceladaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarPartidaCanceladaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarPartidaCanceladaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var partida = await _repo.GetPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            partida = PartidaProyectada.Stub(request.PartidaId, request.SesionPartidaId);
            _repo.AddPartida(partida);
        }
        partida.MarcarCancelada(request.FechaCancelacion);

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PartidaCancelada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
