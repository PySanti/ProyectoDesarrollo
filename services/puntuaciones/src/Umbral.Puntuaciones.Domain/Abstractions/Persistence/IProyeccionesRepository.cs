using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IProyeccionesRepository
{
    Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken);
    void RegistrarEventoProcesado(EventoProcesado evento);
    Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    void AddPartida(PartidaProyectada partida);
    Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
    void AddJuego(JuegoProyectado juego);
    Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken);
    void AddMarcador(Marcador marcador);
    Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken);
}
