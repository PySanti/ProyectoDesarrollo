using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakeHistorialRepository : IHistorialRepository
{
    public List<EventoHistorial> Eventos { get; } = new();

    public Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken)
        => Task.FromResult(Eventos.Any(e => e.EventId == eventId));

    public Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken)
        => Task.FromResult(Eventos.Any(e => e.TipoEvento == "UbicacionActualizada"
            && e.PartidaId == partidaId
            && e.ParticipanteId == participanteId
            && e.OccurredAt > occurredAt - ventana
            && e.OccurredAt < occurredAt + ventana));

    public void AddEvento(EventoHistorial evento) => Eventos.Add(evento);

    public Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken)
        => Task.FromResult(Filtrar(partidaId, tipoEvento).Count());

    public Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<EventoHistorial>>(Filtrar(partidaId, tipoEvento)
            .OrderBy(e => e.OccurredAt)
            .Skip(offset)
            .Take(limit)
            .ToList());

    public Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionEquipoHistorial>>(Eventos
            .Where(e => e.ParticipanteId == participanteId
                && e.EquipoId != null
                && e.TipoEvento != "ConvocatoriaCreada")
            .Select(e => new ParticipacionEquipoHistorial(e.PartidaId, e.EquipoId!.Value))
            .Distinct()
            .ToList());

    private IEnumerable<EventoHistorial> Filtrar(Guid partidaId, string? tipoEvento)
        => Eventos.Where(e => e.PartidaId == partidaId && (tipoEvento == null || e.TipoEvento == tipoEvento));
}
