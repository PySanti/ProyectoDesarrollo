using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

public sealed class ProyectarJuegoActivadoCommandHandler : IRequestHandler<ProyectarJuegoActivadoCommand>
{
    private readonly IProyeccionesRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarJuegoActivadoCommandHandler(IProyeccionesRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarJuegoActivadoCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.EventoYaProcesadoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        var juego = await _repo.GetJuegoAsync(request.JuegoId, cancellationToken);
        if (juego is null)
        {
            _repo.AddJuego(JuegoProyectado.Desde(request.JuegoId, request.PartidaId, request.Orden, request.TipoJuego));
        }

        _repo.RegistrarEventoProcesado(EventoProcesado.Registrar(
            request.EventId, "JuegoActivado", request.OccurredAt, DateTime.UtcNow));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
