using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

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

    public Task<IReadOnlyList<Marcador>> GetMarcadoresDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Marcador>>(Marcadores.Where(m => m.PartidaId == partidaId).ToList());

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Equipo
                && Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == equipoId
                    && m.TipoCompetidor == TipoCompetidor.Equipo))
            .OrderByDescending(p => p.FechaFin)
            .ToList());

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConMarcadorDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada
                && p.Modalidad == Modalidad.Individual
                && Marcadores.Any(m => m.PartidaId == p.PartidaId
                    && m.CompetidorId == participanteId
                    && m.TipoCompetidor == TipoCompetidor.Participante))
            .OrderByDescending(p => p.FechaFin)
            .ToList());

    public Task<IReadOnlyList<JuegoProyectado>> GetJuegosDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<JuegoProyectado>>(Juegos
            .Where(j => j.PartidaId == partidaId)
            .OrderBy(j => j.Orden)
            .ToList());

    public Task<int> EliminarEventosProcesadosAnterioresAsync(DateTime limite, CancellationToken cancellationToken)
    {
        var eliminados = EventosProcesados.RemoveAll(e => e.ProcesadoAt < limite);
        return Task.FromResult(eliminados);
    }

    public List<ParticipacionProyectada> Participaciones { get; } = new();
    public List<ConvocatoriaProyectada> Convocatorias { get; } = new();

    public Task<ParticipacionProyectada?> GetParticipacionAsync(Guid partidaId, Guid competidorId, CancellationToken cancellationToken)
        => Task.FromResult(Participaciones.FirstOrDefault(p => p.PartidaId == partidaId && p.CompetidorId == competidorId));

    public void AddParticipacion(ParticipacionProyectada participacion) => Participaciones.Add(participacion);

    public Task<IReadOnlyList<ParticipacionProyectada>> GetParticipacionesDePartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionProyectada>>(Participaciones.Where(p => p.PartidaId == partidaId).ToList());

    public Task<ConvocatoriaProyectada?> GetConvocatoriaAsync(Guid convocatoriaId, CancellationToken cancellationToken)
        => Task.FromResult(Convocatorias.FirstOrDefault(c => c.ConvocatoriaId == convocatoriaId));

    public void AddConvocatoria(ConvocatoriaProyectada convocatoria) => Convocatorias.Add(convocatoria);

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeParticipanteAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada && p.Modalidad == Modalidad.Individual
                && Participaciones.Any(x => x.PartidaId == p.PartidaId && x.CompetidorId == participanteId
                    && x.TipoCompetidor == TipoCompetidor.Participante))
            .ToList());

    public Task<IReadOnlyList<PartidaProyectada>> GetPartidasTerminadasConParticipacionDeEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartidaProyectada>>(Partidas
            .Where(p => p.Estado == EstadoPartidaProyectada.Terminada && p.Modalidad == Modalidad.Equipo
                && Participaciones.Any(x => x.PartidaId == p.PartidaId && x.CompetidorId == equipoId
                    && x.TipoCompetidor == TipoCompetidor.Equipo))
            .ToList());

    public Task<IReadOnlyList<ParticipacionEquipoHistorial>> GetEquiposConConvocatoriaAceptadaAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<ParticipacionEquipoHistorial>>(Convocatorias
            .Where(c => c.UsuarioId == usuarioId && c.Aceptada)
            .Select(c => new ParticipacionEquipoHistorial(c.PartidaId, c.EquipoId))
            .Distinct()
            .ToList());
}
