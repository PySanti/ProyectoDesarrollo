using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialRepositoryTests
{
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    private static DbContextOptions<PuntuacionesDbContext> Opciones(string db)
        => new DbContextOptionsBuilder<PuntuacionesDbContext>().UseInMemoryDatabase(db).Options;

    private static EventoHistorial Evento(
        Guid partidaId, string tipo, DateTime occurredAt,
        Guid? participanteId = null, Guid? equipoId = null, Guid? juegoId = null)
        => EventoHistorial.Registrar(
            Guid.NewGuid(), partidaId, juegoId, tipo, occurredAt, participanteId, equipoId, "{}");

    [Fact]
    public async Task Inserta_y_lee_historial_de_partida_en_orden_cronologico()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            repo.AddEvento(Evento(partidaId, "PartidaIniciada", Ahora.AddMinutes(1)));
            repo.AddEvento(Evento(partidaId, "PartidaPublicadaEnLobby", Ahora));
            repo.AddEvento(Evento(Guid.NewGuid(), "PartidaIniciada", Ahora));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repoLectura = new HistorialRepository(lectura);
        var entradas = await repoLectura.GetHistorialDePartidaAsync(partidaId, null, 100, 0, CancellationToken.None);
        var total = await repoLectura.ContarHistorialDePartidaAsync(partidaId, null, CancellationToken.None);

        Assert.Equal(2, total);
        Assert.Equal(new[] { "PartidaPublicadaEnLobby", "PartidaIniciada" },
            entradas.Select(e => e.TipoEvento).ToArray());
    }

    [Fact]
    public async Task Paginacion_y_filtro_por_tipo()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            for (var i = 0; i < 5; i++)
            {
                repo.AddEvento(Evento(partidaId, "UbicacionActualizada", Ahora.AddMinutes(i)));
            }
            repo.AddEvento(Evento(partidaId, "EtapaBDTGanada", Ahora.AddMinutes(9)));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repoLectura = new HistorialRepository(lectura);

        var pagina = await repoLectura.GetHistorialDePartidaAsync(partidaId, null, 2, 2, CancellationToken.None);
        Assert.Equal(2, pagina.Count);
        Assert.Equal(Ahora.AddMinutes(2), pagina[0].OccurredAt);

        var filtrado = await repoLectura.GetHistorialDePartidaAsync(partidaId, "EtapaBDTGanada", 100, 0, CancellationToken.None);
        Assert.Single(filtrado);
        Assert.Equal(1, await repoLectura.ContarHistorialDePartidaAsync(partidaId, "EtapaBDTGanada", CancellationToken.None));
    }

    [Fact]
    public void El_modelo_define_indice_unico_por_EventId()
    {
        // El proveedor InMemory no aplica índices únicos no-PK entre contextos: la aplicación real
        // la garantiza PostgreSQL con el DDL de la migración (misma doctrina que xmin, solo-Npgsql).
        // Aquí se protege la configuración del modelo contra regresiones.
        using var db = new PuntuacionesDbContext(Opciones($"historial-{Guid.NewGuid()}"));

        var indice = db.Model.FindEntityType(typeof(EventoHistorial))!
            .GetIndexes()
            .Single(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(EventoHistorial.EventId));

        Assert.True(indice.IsUnique);
    }

    [Fact]
    public async Task ExisteUbicacionCercana_detecta_solo_misma_partida_y_participante_dentro_de_la_ventana()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            new HistorialRepository(db).AddEvento(
                Evento(partidaId, "UbicacionActualizada", Ahora, participanteId));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var repo = new HistorialRepository(lectura);
        var ventana = TimeSpan.FromSeconds(60);

        Assert.True(await repo.ExisteUbicacionCercanaAsync(partidaId, participanteId, Ahora.AddSeconds(30), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(partidaId, participanteId, Ahora.AddSeconds(90), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(partidaId, Guid.NewGuid(), Ahora.AddSeconds(30), ventana, CancellationToken.None));
        Assert.False(await repo.ExisteUbicacionCercanaAsync(Guid.NewGuid(), participanteId, Ahora.AddSeconds(30), ventana, CancellationToken.None));
    }

    [Fact]
    public async Task GetEquiposDelParticipante_resuelve_membresia_excluyendo_ConvocatoriaCreada()
    {
        var opciones = Opciones($"historial-{Guid.NewGuid()}");
        var participanteId = Guid.NewGuid();
        var partidaJugada = Guid.NewGuid();
        var partidaRechazada = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new HistorialRepository(db);
            // Dos acciones de juego en la misma partida → una sola participación (Distinct).
            repo.AddEvento(Evento(partidaJugada, "RespuestaTriviaValidada", Ahora, participanteId, equipoId));
            repo.AddEvento(Evento(partidaJugada, "TesoroQRValidado", Ahora.AddMinutes(1), participanteId, equipoId));
            // Convocatoria sin acción de juego → no cuenta como participación.
            repo.AddEvento(Evento(partidaRechazada, "ConvocatoriaCreada", Ahora, participanteId, equipoId));
            // Evento sin equipo → no cuenta.
            repo.AddEvento(Evento(partidaJugada, "UbicacionActualizada", Ahora, participanteId));
            await db.SaveChangesAsync();
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var participaciones = await new HistorialRepository(lectura)
            .GetEquiposDelParticipanteAsync(participanteId, CancellationToken.None);

        var participacion = Assert.Single(participaciones);
        Assert.Equal(partidaJugada, participacion.PartidaId);
        Assert.Equal(equipoId, participacion.EquipoId);
    }
}
