using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarConvocatoriaRespondidaCommandHandler : IRequestHandler<ProyectarConvocatoriaRespondidaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarConvocatoriaRespondidaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarConvocatoriaRespondidaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        // Si se perdio ConvocatoriaCreada no hay fila que actualizar y no se puede crear: este
        // evento no trae EquipoId. Se ackea (best-effort ADR-0012) y el miembro cae al
        // comportamiento previo — solo ve la partida si actuo.
        var convocatoria = await _repo.GetConvocatoriaAsync(request.ConvocatoriaId, cancellationToken);
        convocatoria?.Responder(request.EstadoConvocatoria == "Aceptada");

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "ConvocatoriaRespondida", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
