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

public class SesionPartidaRepositoryEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

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
    public async Task GetByConvocatoriaId_encuentra_la_sesion()
    {
        await using var ctx = NewCtx("equipo-conv-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { Guid.NewGuid() }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        var convocatoriaId = insc.Convocatorias[0].Id.Valor;
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.GetByConvocatoriaIdAsync(convocatoriaId, CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(sesion.PartidaId, r!.PartidaId);
        Assert.Contains(r.Inscripciones.SelectMany(i => i.Convocatorias), c => c.Id.Valor == convocatoriaId);
    }

    [Fact]
    public async Task EquipoTieneParticipacionActiva_detecta_en_otra_partida()
    {
        await using var ctx = NewCtx("equipo-act-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var equipoId = Guid.NewGuid();
        var otra = PartidaEquipo(Guid.NewGuid());
        otra.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid() }, false, 0, T0);
        repo.Add(otra);
        await ctx.SaveChangesAsync();

        var r = await repo.EquipoTieneParticipacionActivaAsync(equipoId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(r);
    }

    [Fact]
    public async Task ParticipanteConConvocatoriaAceptada_cuenta_como_participacion_activa()
    {
        await using var ctx = NewCtx("equipo-partact-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var usuario = Guid.NewGuid();
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.ParticipanteTieneParticipacionActivaAsync(usuario, Guid.NewGuid(), CancellationToken.None);

        Assert.True(r);
    }

    [Fact]
    public async Task MiSesion_encuentra_sesion_por_convocatoria_aceptada()
    {
        await using var ctx = NewCtx("equipo-misesion-" + Guid.NewGuid());
        var repo = new SesionPartidaRepository(ctx);
        var usuario = Guid.NewGuid();
        var sesion = PartidaEquipo(Guid.NewGuid());
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
        repo.Add(sesion);
        await ctx.SaveChangesAsync();

        var r = await repo.GetByParticipanteActivoAsync(usuario, CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(sesion.PartidaId, r!.PartidaId);
    }

    [Fact]
    public async Task GetByPartidaId_carga_Convocatorias_de_las_inscripciones()
    {
        // Cheap addition (B11 review): GetByPartidaIdAsync ya trae .ThenInclude(i => i.Convocatorias)
        // desde B10, pero no había una aserción directa. Escribe/lee con contextos EF distintos
        // para que el Include realmente se ejerza (no la identidad ya trackeada en memoria).
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("equipo-getbypartida-" + Guid.NewGuid()).Options;
        var usuario = Guid.NewGuid();
        var partidaId = Guid.NewGuid();

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var sesion = PartidaEquipo(partidaId);
            var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { usuario }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            sesion.ResponderConvocatoria(insc.Convocatorias[0].Id.Valor, usuario, true, false, T0);
            new SesionPartidaRepository(write).Add(sesion);
            await write.SaveChangesAsync();
        }

        await using var read = new OperacionesSesionDbContext(options);
        var r = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);

        Assert.NotNull(r);
        Assert.NotEmpty(r!.Inscripciones.SelectMany(i => i.Convocatorias));
    }
}
