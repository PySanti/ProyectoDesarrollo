using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarConvocatoriaCreadaCommandHandler : IRequestHandler<ProyectarConvocatoriaCreadaCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarConvocatoriaCreadaCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarConvocatoriaCreadaCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        if (await _repo.GetConvocatoriaAsync(request.ConvocatoriaId, cancellationToken) is null)
        {
            _repo.AddConvocatoria(ConvocatoriaProyectada.Nueva(
                request.ConvocatoriaId, request.PartidaId, request.EquipoId, request.UsuarioId));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "ConvocatoriaCreada", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
