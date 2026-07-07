using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Commands;

// Dedup por EventId contra la propia tabla (sin tocar eventos_procesados, que pertenece al
// consumidor de proyecciones). Muestreo de ubicaciones: cota de volumen best-effort, no invariante
// (la carrera residual del check-then-insert la cubre el índice único; la trata el consumidor).
public sealed class ProyectarEventoHistorialCommandHandler : IRequestHandler<ProyectarEventoHistorialCommand>
{
    private const string TipoUbicacion = "UbicacionActualizada";
    private static readonly TimeSpan VentanaMuestreoUbicacion = TimeSpan.FromSeconds(60);

    private readonly IHistorialRepository _repo;
    private readonly IPuntuacionesUnitOfWork _uow;

    public ProyectarEventoHistorialCommandHandler(IHistorialRepository repo, IPuntuacionesUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(ProyectarEventoHistorialCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExisteEventoAsync(request.EventId, cancellationToken))
        {
            return;
        }

        if (request.TipoEvento == TipoUbicacion
            && request.ParticipanteId is { } participanteId
            && await _repo.ExisteUbicacionCercanaAsync(
                request.PartidaId, participanteId, request.OccurredAt, VentanaMuestreoUbicacion, cancellationToken))
        {
            return;
        }

        _repo.AddEvento(EventoHistorial.Registrar(
            request.EventId, request.PartidaId, request.JuegoId, request.TipoEvento,
            request.OccurredAt, request.ParticipanteId, request.EquipoId, request.DetalleJson));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
