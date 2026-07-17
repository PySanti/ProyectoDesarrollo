using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class SesionPartidaRepository : ISesionPartidaRepository
{
    private readonly OperacionesSesionDbContext _dbContext;

    public SesionPartidaRepository(OperacionesSesionDbContext dbContext) => _dbContext = dbContext;

    public void Add(SesionPartida sesion) => _dbContext.Sesiones.Add(sesion);

    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
            .FirstOrDefaultAsync(s => s.PartidaId == partidaId, cancellationToken);

    public Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones.AnyAsync(s => s.PartidaId == partidaId, cancellationToken);

    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            // BR-G09 (HU-19): Pendiente+Activa ocupan participación para la inscripción propia.
            // La convocatoria aceptada solo cuenta sobre inscripciones ya activas.
            .AnyAsync(i =>
                ((i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa)
                    && i.ParticipanteId == participanteId)
                || (i.Estado == EstadoInscripcion.Activa
                    && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada)),
                cancellationToken);

    public Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
            .Where(s => s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada)
            .OrderBy(s => s.PartidaId)
            // mi-sesion debe mostrar también el estado Pendiente de la inscripción propia;
            // la convocatoria aceptada sigue exigiendo inscripción activa (BR-G09, HU-19).
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i =>
                    ((i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa)
                        && i.ParticipanteId == participanteId)
                    || (i.Estado == EstadoInscripcion.Activa
                        && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.Estado == EstadoConvocatoria.Aceptada))),
                cancellationToken);

    public async Task<IReadOnlyList<SesionPartida>> GetSesionesConActividadVencidaAsync(
        DateTime now, CancellationToken cancellationToken)
    {
        var iniciadas = await _dbContext.Sesiones
            // Opciones es obligatorio: BarrerTimeoutsCommandHandler lee
            // preguntaCerrada.Opciones.First(o => o.EsCorrecta) sobre este mismo grafo para
            // publicar el cierre; sin Include quedaría vacía en Npgsql (sin lazy loading) →
            // InvalidOperationException en todo cierre de pregunta Trivia por timeout (mismo
            // patrón que GetByPartidaIdAsync).
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas)
            .Where(s => s.Estado == EstadoSesion.Iniciada)
            .ToListAsync(cancellationToken);

        return iniciadas
            .Where(s => TienePasoVencido(s, now))
            .ToList();
    }

    public async Task<IReadOnlyList<SesionPartida>> GetSesionesAutoInicioPendienteAsync(
        DateTime now, CancellationToken cancellationToken)
        => await _dbContext.Sesiones
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
            // Convocatorias es obligatorio: AplicarInicio calcula el quorum de Equipo contando
            // ConvocatoriasAceptadas (campo respaldado por _convocatorias); sin Include quedaría
            // vacía → quorum 0 → cancelación automática incorrecta de partidas Equipo con cupo.
            // Preguntas/Etapas son obligatorias: AplicarInicio activa el primer paso del primer
            // juego iterando _preguntas/_etapas; sin Include quedarían vacías en Npgsql (sin lazy
            // loading) → juego Activo SIN paso activo → partida atascada (mismo grafo que GetByPartidaIdAsync).
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas)
            .Where(s => s.Estado == EstadoSesion.Lobby
                && (s.ModoInicioPartida == ModoInicioPartida.Automatico
                    || s.ModoInicioPartida == ModoInicioPartida.ManualYAutomatico)
                && s.TiempoInicio != null
                && s.TiempoInicio <= now)
            .ToListAsync(cancellationToken);

    public Task<bool> EquipoTieneParticipacionActivaAsync(
        Guid equipoId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            // BR-G09 (HU-19): Pendiente+Activa ocupan participación del equipo.
            .AnyAsync(i => i.EquipoId == equipoId
                && (i.Estado == EstadoInscripcion.Pendiente || i.Estado == EstadoInscripcion.Activa),
                cancellationToken);

    public Task<SesionPartida?> GetByConvocatoriaIdAsync(Guid convocatoriaId, CancellationToken cancellationToken)
    {
        var id = ConvocatoriaId.From(convocatoriaId);
        return _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones).ThenInclude(i => i.Convocatorias)
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i => i.Convocatorias.Any(c => c.Id == id)),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ConvocatoriaPendienteProyeccion>> GetConvocatoriasPendientesByUsuarioAsync(
        Guid usuarioId, CancellationToken cancellationToken)
    {
        // Proyeccion anonima entidad+escalar y mapeo en memoria: proyectar c.Id.Valor
        // directo en la consulta no traduce con el value object del id.
        var filas = await _dbContext.Sesiones
            .Where(s => s.Estado == EstadoSesion.Lobby)
            .SelectMany(s => s.Inscripciones
                .Where(i => i.Estado == EstadoInscripcion.Activa)
                .SelectMany(i => i.Convocatorias
                    .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoConvocatoria.Pendiente)
                    .Select(c => new { Convocatoria = c, s.Nombre })))
            .ToListAsync(cancellationToken);

        return filas
            .Select(f => new ConvocatoriaPendienteProyeccion(
                f.Convocatoria.Id.Valor, f.Convocatoria.PartidaId, f.Nombre,
                f.Convocatoria.EquipoId, f.Convocatoria.FechaEnvio))
            .OrderBy(x => x.FechaEnvio)
            .ToList();
    }

    public async Task<IReadOnlyList<SesionPartida>> GetSesionesEnLobbyAsync(CancellationToken cancellationToken)
        => await _dbContext.Sesiones
            .AsNoTracking()
            .Where(s => s.Estado == EstadoSesion.Lobby)
            // Solo Inscripciones: el listado cuenta activas; no necesita convocatorias ni juegos.
            .Include(s => s.Inscripciones)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NombrePartidaProyeccion>> GetNombresByPartidaIdsAsync(
        IReadOnlyList<Guid> partidaIds, CancellationToken cancellationToken)
        // Proyeccion directa al record: PartidaId y Nombre son escalares planos, asi que
        // traduce sin el mapeo en memoria que exige GetConvocatoriasPendientesByUsuarioAsync
        // (alli el id es un value object).
        => await _dbContext.Sesiones
            .Where(s => partidaIds.Contains(s.PartidaId))
            .Select(s => new NombrePartidaProyeccion(s.PartidaId, s.Nombre))
            .ToListAsync(cancellationToken);

    private static bool TienePasoVencido(SesionPartida sesion, DateTime now)
    {
        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        if (juego is null) return false;
        var pregunta = juego.PreguntaActiva;
        if (pregunta is not null)
            return now >= pregunta.FechaActivacion!.Value.AddSeconds(pregunta.TiempoLimiteSegundos);
        var etapa = juego.EtapaActiva;
        if (etapa is not null)
            return now >= etapa.FechaActivacion!.Value.AddSeconds(etapa.TiempoLimiteSegundos);
        return false;
    }
}
