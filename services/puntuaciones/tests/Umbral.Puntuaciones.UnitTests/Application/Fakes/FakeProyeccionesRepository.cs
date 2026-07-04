using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.UnitTests.Application.Fakes;

public sealed class FakeProyeccionesRepository : IProyeccionesRepository
{
    public List<PartidaProyectada> Partidas { get; } = new();
    public List<JuegoProyectado> Juegos { get; } = new();
    public List<Marcador> Marcadores { get; } = new();
    public List<EventoProcesado> EventosProcesados { get; } = new();

    public Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken)
        => Task.FromResult(EventosProcesados.Any(e => e.EventId == eventId));

    public void RegistrarEventoProcesado(EventoProcesado evento) => EventosProcesados.Add(evento);

    public Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(Partidas.FirstOrDefault(p => p.PartidaId == partidaId));

    public void AddPartida(PartidaProyectada partida) => Partidas.Add(partida);

    public Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => Task.FromResult(Juegos.FirstOrDefault(j => j.JuegoId == juegoId));

    public void AddJuego(JuegoProyectado juego) => Juegos.Add(juego);

    public Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
        => Task.FromResult(Marcadores.FirstOrDefault(m => m.JuegoId == juegoId && m.CompetidorId == competidorId));

    public void AddMarcador(Marcador marcador) => Marcadores.Add(marcador);

    public Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Marcador>>(Marcadores.Where(m => m.JuegoId == juegoId).ToList());
}
