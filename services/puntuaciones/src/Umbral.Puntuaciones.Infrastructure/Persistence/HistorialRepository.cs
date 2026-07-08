using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class HistorialRepository : IHistorialRepository
{
    private const string TipoUbicacion = "UbicacionActualizada";
    private const string TipoConvocatoriaCreada = "ConvocatoriaCreada";

    private readonly PuntuacionesDbContext _db;

    public HistorialRepository(PuntuacionesDbContext db) => _db = db;

    public Task<bool> ExisteEventoAsync(Guid eventId, CancellationToken cancellationToken)
        => _db.EventosHistorial.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);

    public Task<bool> ExisteUbicacionCercanaAsync(
        Guid partidaId, Guid participanteId, DateTime occurredAt, TimeSpan ventana, CancellationToken cancellationToken)
    {
        var desde = occurredAt - ventana;
        var hasta = occurredAt + ventana;
        return _db.EventosHistorial.AsNoTracking().AnyAsync(
            e => e.TipoEvento == TipoUbicacion
                && e.PartidaId == partidaId
                && e.ParticipanteId == participanteId
                && e.OccurredAt > desde
                && e.OccurredAt < hasta,
            cancellationToken);
    }

    public void AddEvento(EventoHistorial evento) => _db.EventosHistorial.Add(evento);

    public Task<int> ContarHistorialDePartidaAsync(Guid partidaId, string? tipoEvento, CancellationToken cancellationToken)
        => FiltrarPorPartida(partidaId, tipoEvento).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<EventoHistorial>> GetHistorialDePartidaAsync(
        Guid partidaId, string? tipoEvento, int limit, int offset, CancellationToken cancellationToken)
        => await FiltrarPorPartida(partidaId, tipoEvento)
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    // Membresía HU-27: acciones de juego autoradas acreditadas a un equipo. ConvocatoriaCreada se
    // excluye para no listar convocados que rechazaron (design SP-4d §4).
    public async Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposDelParticipanteAsync(
        Guid participanteId, CancellationToken cancellationToken)
    {
        var filas = await _db.EventosHistorial.AsNoTracking()
            .Where(e => e.ParticipanteId == participanteId
                && e.EquipoId != null
                && e.TipoEvento != TipoConvocatoriaCreada)
            .Select(e => new { e.PartidaId, e.EquipoId })
            .Distinct()
            .ToListAsync(cancellationToken);
        return filas.Select(f => new ParticipacionEquipoHistorial(f.PartidaId, f.EquipoId!.Value)).ToList();
    }

    private IQueryable<EventoHistorial> FiltrarPorPartida(Guid partidaId, string? tipoEvento)
    {
        var query = _db.EventosHistorial.AsNoTracking().Where(e => e.PartidaId == partidaId);
        return tipoEvento is null ? query : query.Where(e => e.TipoEvento == tipoEvento);
    }
}
