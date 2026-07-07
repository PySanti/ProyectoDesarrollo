using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarPartidaFinalizadaCommandHandler : IRequestHandler<ProyectarPartidaFinalizadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarPartidaFinalizadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarPartidaFinalizadaCommand request, CancellationToken cancellationToken)
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
        partida.MarcarTerminada(request.FechaFin);

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "PartidaFinalizada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
