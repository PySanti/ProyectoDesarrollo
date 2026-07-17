using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Domain.Abstractions.Persistence;

public interface IProyeccionesRepository
{
    Task<bool> EventoYaProcesadoAsync(Guid eventId, CancellationToken cancellationToken);
    void RegistrarEventoProcesado(EventoProcesado evento);
    Task<int> EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken cancellationToken);
    Task<PartidaProyectada?> GetPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    void AddPartida(PartidaProyectada partida);
    Task<JuegoProyectado?> GetJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
    void AddJuego(JuegoProyectado juego);
    Task<Marcador?> GetMarcadorAsync(Guid juegoId, Guid competidorId, CancellationToken cancellationToken);
    void AddMarcador(Marcador marcador);
    Task<IReadOnlyList<Marcador>> GetMarcadoresDeJuegoAsync(Guid juegoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JuegoProyectado>> GetJuegosDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);

    // Participación = inscripción aceptada, no haber anotado. Antes el único universo de
    // competidores era el de marcadores, y un marcador solo nace al acreditar puntos.
    Task<ParticipacionProyectada?> GetParticipacionAsync(Guid partidaId, Guid competidorId, CancellationToken cancellationToken);
    void AddParticipacion(ParticipacionProyectada participacion);
    Task<IReadOnlyList<ParticipacionProyectada>> GetParticipacionesDePartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<ConvocatoriaProyectada?> GetConvocatoriaAsync(Guid convocatoriaId, CancellationToken cancellationToken);
    void AddConvocatoria(ConvocatoriaProyectada convocatoria);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposConConvocatoriaAceptadaAsync(Guid usuarioId, CancellationToken cancellationToken);
}
