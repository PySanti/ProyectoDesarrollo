using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class ProyeccionesRepository : IProyeccionesRepository
{
    private readonly PuntuacionesDbContext _db;

    public ProyeccionesRepository(PuntuacionesDbContext db) => _db = db;

    public Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken)
        => _db.EventosProcesados.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);

    public void RegistrarEventoProcesado(EventoProcesado evento) => _db.EventosProcesados.Add(evento);

    public Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => _db.Partidas.FirstOrDefaultAsync(p => p.PartidaId == partidaId, cancellationToken);

    public void AddPartida(PartidaProyectada partida) => _db.Partidas.Add(partida);

    public Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => _db.Juegos.FirstOrDefaultAsync(j => j.JuegoId == juegoId, cancellationToken);

    public void AddJuego(JuegoProyectado juego) => _db.Juegos.Add(juego);

    public Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
        => _db.Marcadores.FirstOrDefaultAsync(
            m => m.JuegoId == juegoId && m.CompetidorId == competidorId, cancellationToken);

    public void AddMarcador(Marcador marcador) => _db.Marcadores.Add(marcador);

    public async Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => await _db.Marcadores.AsNoTracking()
            .Where(m => m.JuegoId == juegoId)
            .ToListAsync(cancellationToken);
}
