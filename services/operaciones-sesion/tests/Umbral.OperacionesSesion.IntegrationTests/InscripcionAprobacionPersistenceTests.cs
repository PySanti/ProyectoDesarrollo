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

public class InscripcionAprobacionPersistenceTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipo(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Preinscripcion_pendiente_persiste_snapshot_de_miembros()
    {
        // Contextos EF distintos para write/read: así el snapshot se materializa desde la
        // columna persistida, no desde la identidad ya trackeada en memoria.
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("insc-aprob-snap-" + Guid.NewGuid()).Options;
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var sesion = PartidaEquipo(partidaId);
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, m1, new[] { m1, m2 }, false, 0, T0);
            new SesionPartidaRepository(write).Add(sesion);
            await write.SaveChangesAsync();
        }

        await using var read = new OperacionesSesionDbContext(options);
        var r = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);

        Assert.NotNull(r);
        var insc = Assert.Single(r!.Inscripciones);
        Assert.Equal(EstadoInscripcion.Pendiente, insc.Estado);
        Assert.Empty(insc.Convocatorias);
        Assert.Equal(new[] { m1, m2 }, insc.MiembrosSnapshot);
    }

    // LiderId tiene que sobrevivir al round-trip: las convocatorias se crean al aceptar el
    // operador, que ocurre en otra request y sobre la entidad recargada. Si no se persiste, vuelve
    // como Guid.Empty y el lider no se auto-acepta — la partida se cancelaria por minimos.
    [Fact]
    public async Task Preinscripcion_persiste_el_lider_y_su_convocatoria_nace_aceptada_tras_recargar()
    {
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("insc-aprob-lider-" + Guid.NewGuid()).Options;
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        Guid inscId;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var sesion = PartidaEquipo(partidaId);
            var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider, miembro }, false, 0, T0);
            inscId = insc.Id.Valor;
            new SesionPartidaRepository(write).Add(sesion);
            await write.SaveChangesAsync();
        }

        await using var read = new OperacionesSesionDbContext(options);
        var recargada = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);
        var reinsc = Assert.Single(recargada!.Inscripciones);

        Assert.Equal(lider, reinsc.LiderId);

        recargada.AceptarInscripcion(inscId, 0, T0, liderPuedeAutoAceptar: true);

        Assert.True(reinsc.Convocatorias.Single(c => c.UsuarioId == lider).EstaAceptada);
        Assert.True(reinsc.Convocatorias.Single(c => c.UsuarioId == miembro).EstaPendiente);
    }

    [Fact]
    public async Task Aceptar_tras_recargar_crea_convocatorias_desde_snapshot()
    {
        // Regresión del ciclo completo: preinscribir (Pendiente) → persistir → recargar →
        // aceptar. Aceptar debe reconstruir las convocatorias desde el snapshot persistido.
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("insc-aprob-aceptar-" + Guid.NewGuid()).Options;
        var partidaId = Guid.NewGuid();
        var m1 = Guid.NewGuid();

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var sesion = PartidaEquipo(partidaId);
            sesion.PreinscribirEquipo(Guid.NewGuid(), true, m1, new[] { m1 }, false, 0, T0);
            new SesionPartidaRepository(write).Add(sesion);
            await write.SaveChangesAsync();
        }

        await using var ctx = new OperacionesSesionDbContext(options);
        var repo = new SesionPartidaRepository(ctx);
        var recargada = await repo.GetByPartidaIdAsync(partidaId, CancellationToken.None);
        var insc = recargada!.Inscripciones.Single();

        var creadas = recargada.AceptarInscripcion(insc.Id.Valor, 0, T0);
        await ctx.SaveChangesAsync();

        var c = Assert.Single(creadas);
        Assert.Equal(m1, c.UsuarioId);
        Assert.Equal(partidaId, c.PartidaId);
        Assert.Equal(EstadoInscripcion.Activa, insc.Estado);
    }
}
