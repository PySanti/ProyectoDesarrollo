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

public class ConvocatoriasPendientesScanTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    private static SesionPartida EquipoPublicada()
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task Devuelve_pendiente_de_sesion_en_lobby()
    {
        var db = "convo-pend-" + Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var sesion = EquipoPublicada();
            partidaId = sesion.PartidaId;
            var insc = sesion.PreinscribirEquipo(equipo, true, new[] { usuario }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetConvocatoriasPendientesByUsuarioAsync(usuario, CancellationToken.None);
            var c = Assert.Single(r);
            Assert.Equal(partidaId, c.PartidaId);
            Assert.Equal(equipo, c.EquipoId);
            // El estado ya no viaja en la proyeccion: el filtro Pendiente es invariante del
            // metodo y lo cubren Excluye_respondidas / Excluye_sesion_iniciada.
            Assert.NotEqual(Guid.Empty, c.ConvocatoriaId);
        }
    }

    [Fact]
    public async Task Trae_el_nombre_de_la_sesion_junto_a_la_convocatoria()
    {
        // El nombre vive en SesionPartida; el SelectMany hasta Convocatoria lo perdia.
        var db = "convo-pend-" + Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var usuario = Guid.NewGuid();

        await using (var ctx = NewCtx(db))
        {
            var sesion = EquipoPublicada();
            var insc = sesion.PreinscribirEquipo(equipo, true, new[] { usuario }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0);
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetConvocatoriasPendientesByUsuarioAsync(usuario, CancellationToken.None);

            Assert.Equal("Copa", Assert.Single(r).NombrePartida);
        }
    }

    [Fact]
    public async Task Excluye_sesion_iniciada()
    {
        var db = "convo-pend-" + Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var otroMiembro = Guid.NewGuid();
        var usuario = Guid.NewGuid();

        await using (var ctx = NewCtx(db))
        {
            var sesion = EquipoPublicada();
            var insc = sesion.PreinscribirEquipo(equipo, true, new[] { otroMiembro, usuario }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            sesion.ResponderConvocatoria(
                insc.Convocatorias.Single(c => c.UsuarioId == otroMiembro).Id.Valor, otroMiembro, true, false, T0);
            sesion.Iniciar(T0); // otroMiembro aceptó → quorum cumplido (minimo=1); usuario queda Pendiente
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetConvocatoriasPendientesByUsuarioAsync(usuario, CancellationToken.None);
            Assert.Empty(r);
        }
    }

    [Fact]
    public async Task Excluye_respondidas()
    {
        var db = "convo-pend-" + Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var aceptante = Guid.NewGuid();
        var rechazante = Guid.NewGuid();

        await using (var ctx = NewCtx(db))
        {
            var sesion = EquipoPublicada();
            var insc = sesion.PreinscribirEquipo(equipo, true, new[] { aceptante, rechazante }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            sesion.ResponderConvocatoria(
                insc.Convocatorias.Single(c => c.UsuarioId == aceptante).Id.Valor, aceptante, true, false, T0);
            sesion.ResponderConvocatoria(
                insc.Convocatorias.Single(c => c.UsuarioId == rechazante).Id.Valor, rechazante, false, false, T0);
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Empty(await repo.GetConvocatoriasPendientesByUsuarioAsync(aceptante, CancellationToken.None));
            Assert.Empty(await repo.GetConvocatoriasPendientesByUsuarioAsync(rechazante, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Excluye_de_otros_usuarios()
    {
        var db = "convo-pend-" + Guid.NewGuid();
        var equipo = Guid.NewGuid();
        var otroUsuario = Guid.NewGuid();
        var caller = Guid.NewGuid();

        await using (var ctx = NewCtx(db))
        {
            var sesion = EquipoPublicada();
            var insc = sesion.PreinscribirEquipo(equipo, true, new[] { otroUsuario }, false, 0, T0);
            sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetConvocatoriasPendientesByUsuarioAsync(caller, CancellationToken.None);
            Assert.Empty(r);
        }
    }
}
