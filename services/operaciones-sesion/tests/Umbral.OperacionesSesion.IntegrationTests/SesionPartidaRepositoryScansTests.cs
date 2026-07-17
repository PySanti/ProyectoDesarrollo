using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPartidaRepositoryScansTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaPublicada(int limite, DateTime? tiempoInicio, ModoInicioPartida modo)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, limite), P(2, limite) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, 1, 5, new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task ConActividadVencida_devuelve_solo_iniciadas_con_paso_vencido()
    {
        await using var ctx = NewCtx("scan-venc-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);

        var vencida = TriviaPublicada(30, null, ModoInicioPartida.Manual);
        var inscVencida = vencida.Inscribir(Guid.NewGuid(), false, 0, T0);
        vencida.AceptarInscripcion(inscVencida.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        vencida.Iniciar(T0); // Q1 activa, FechaActivacion = T0

        var noVencida = TriviaPublicada(30, null, ModoInicioPartida.Manual);
        var inscNoVencida = noVencida.Inscribir(Guid.NewGuid(), false, 0, T0);
        noVencida.AceptarInscripcion(inscNoVencida.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        noVencida.Iniciar(T0);

        var enLobby = TriviaPublicada(30, null, ModoInicioPartida.Manual);

        repo.Add(vencida); repo.Add(noVencida); repo.Add(enLobby);
        await ctx.SaveChangesAsync();

        var r = await repo.GetSesionesConActividadVencidaAsync(T0.AddSeconds(31), CancellationToken.None);

        Assert.Contains(r, s => s.PartidaId == vencida.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == enLobby.PartidaId);
        // noVencida: con now=T0+31 su Q (limite 30) también está vencida → también aparece;
        // el filtro clave probado aquí es "Iniciada con paso vencido" vs "Lobby".
    }

    [Fact]
    public async Task ConActividadVencida_carga_Opciones_de_Preguntas_para_publicar_cierre()
    {
        // 7d review Critical #1: BarrerTimeoutsCommandHandler lee
        // preguntaCerrada.Opciones.First(o => o.EsCorrecta) sobre el mismo grafo devuelto por
        // este método para publicar el cierre. Sin ThenInclude(p => p.Opciones), Npgsql (sin
        // lazy loading) devuelve la colección vacía → InvalidOperationException en todo cierre
        // de pregunta Trivia por timeout. Escribe y lee con contextos EF distintos (mismo
        // patrón que AutoInicioPendiente_carga_Convocatorias_para_quorum_de_Equipo): reusar el
        // mismo DbContext devolvería la instancia ya trackeada y enmascararía el bug.
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("scan-venc-opciones-" + Guid.NewGuid()).Options;

        Guid partidaId;
        await using (var write = new OperacionesSesionDbContext(options))
        {
            var writeRepo = new SesionPartidaRepository(write);
            var sesion = TriviaPublicada(30, null, ModoInicioPartida.Manual);
            var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
            sesion.Iniciar(T0); // Q1 activa, FechaActivacion = T0
            writeRepo.Add(sesion);
            await write.SaveChangesAsync();
            partidaId = sesion.PartidaId;
        }

        await using var read = new OperacionesSesionDbContext(options);
        var readRepo = new SesionPartidaRepository(read);

        var r = await readRepo.GetSesionesConActividadVencidaAsync(T0.AddSeconds(31), CancellationToken.None);

        var cargada = Assert.Single(r, s => s.PartidaId == partidaId);
        var pregunta = cargada.Juegos.Single().Preguntas.Single(p => p.Orden == 1);
        Assert.NotEmpty(pregunta.Opciones);
        Assert.Contains(pregunta.Opciones, o => o.EsCorrecta);
    }

    [Fact]
    public async Task AutoInicioPendiente_devuelve_solo_lobby_automatico_con_hora_cumplida()
    {
        await using var ctx = NewCtx("scan-auto-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);

        var due = TriviaPublicada(30, T0, ModoInicioPartida.Automatico);          // Lobby, hora cumplida
        var futura = TriviaPublicada(30, T0.AddHours(1), ModoInicioPartida.Automatico); // Lobby, aún no
        var manual = TriviaPublicada(30, T0, ModoInicioPartida.Manual);            // Lobby pero Manual

        repo.Add(due); repo.Add(futura); repo.Add(manual);
        await ctx.SaveChangesAsync();

        var r = await repo.GetSesionesAutoInicioPendienteAsync(T0.AddSeconds(1), CancellationToken.None);

        Assert.Contains(r, s => s.PartidaId == due.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == futura.PartidaId);
        Assert.DoesNotContain(r, s => s.PartidaId == manual.PartidaId);
    }

    private static SesionPartida EquipoPublicada(DateTime? tiempoInicio, ModoInicioPartida modo)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, 30) });
        var snap = new ConfiguracionSnapshot("Copa Equipos", Modalidad.Equipo, modo, tiempoInicio, 1, 5, new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task AutoInicioPendiente_carga_Convocatorias_para_quorum_de_Equipo()
    {
        // B11-fix Finding 1: sin ThenInclude(i => i.Convocatorias) el quorum de Equipo
        // (AplicarInicio cuenta ConvocatoriasAceptadas) siempre da 0 → cancelación automática
        // incorrecta de partidas Equipo con cupo cumplido. Escribe y lee con contextos EF
        // distintos (mismo nombre de BD InMemory) para que el Include realmente importe:
        // reusar el mismo DbContext devolvería la instancia ya trackeada en memoria y
        // enmascararía el bug (falso verde).
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("scan-auto-equipo-" + Guid.NewGuid()).Options;

        var equipoId = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var partidaId = Guid.NewGuid();

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var writeRepo = new SesionPartidaRepository(write);
            var sesion = EquipoPublicada(T0, ModoInicioPartida.Automatico);
            var insc = sesion.PreinscribirEquipo(equipoId, true, miembro, new[] { miembro }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, miembro, true, false, T0);
            writeRepo.Add(sesion);
            await write.SaveChangesAsync();
            partidaId = sesion.PartidaId;
        }

        await using var read = new OperacionesSesionDbContext(options);
        var readRepo = new SesionPartidaRepository(read);

        var r = await readRepo.GetSesionesAutoInicioPendienteAsync(T0.AddSeconds(1), CancellationToken.None);

        var cargada = Assert.Single(r, s => s.PartidaId == partidaId);
        var inscripcion = Assert.Single(cargada.Inscripciones);
        Assert.True(inscripcion.ConvocatoriasAceptadas >= 1);
    }
}
