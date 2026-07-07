using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IHistorialRepository
{
    Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken);
    Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken);
    void AddEvento(EventoHistorial evento);
    Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken);
    Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken);
}
