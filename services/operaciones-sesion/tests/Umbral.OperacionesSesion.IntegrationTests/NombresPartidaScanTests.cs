using System;
using System.Collections.Generic;
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

// Limitacion heredada de todo el suite de persistencia de este servicio: se prueba contra
// el proveedor InMemory, que no valida traduccion a SQL. La traduccion Npgsql de
// partidaIds.Contains(...) no queda verificada aqui ni en ningun otro test del servicio
// (ninguno usa Postgres real). Es un hueco sistemico preexistente, no de este metodo.
public class NombresPartidaScanTests
{
    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    private static SesionPartida Publicada(string nombre)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot(nombre, Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { juego });
        return SesionPartida.Publicar(Guid.NewGuid(), snap);
    }

    [Fact]
    public async Task Resuelve_nombres_de_partidas_publicadas()
    {
        var db = "nombres-partida-" + Guid.NewGuid();
        Guid copaId, ligaId;

        await using (var ctx = NewCtx(db))
        {
            var copa = Publicada("Copa UMBRAL");
            var liga = Publicada("Liga UCAB");
            copaId = copa.PartidaId;
            ligaId = liga.PartidaId;
            var repo = new SesionPartidaRepository(ctx);
            repo.Add(copa);
            repo.Add(liga);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx)
                .GetNombresByPartidaIdsAsync(new[] { copaId, ligaId }, CancellationToken.None);

            Assert.Equal(2, r.Count);
            Assert.Equal("Copa UMBRAL", r.Single(x => x.PartidaId == copaId).Nombre);
            Assert.Equal("Liga UCAB", r.Single(x => x.PartidaId == ligaId).Nombre);
        }
    }

    [Fact]
    public async Task Solo_devuelve_los_ids_pedidos()
    {
        var db = "nombres-partida-" + Guid.NewGuid();
        Guid copaId;

        await using (var ctx = NewCtx(db))
        {
            var copa = Publicada("Copa UMBRAL");
            copaId = copa.PartidaId;
            var repo = new SesionPartidaRepository(ctx);
            repo.Add(copa);
            repo.Add(Publicada("Liga UCAB")); // no se pide: no debe volver
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx)
                .GetNombresByPartidaIdsAsync(new[] { copaId }, CancellationToken.None);

            Assert.Equal("Copa UMBRAL", Assert.Single(r).Nombre);
        }
    }

    [Fact]
    public async Task Id_desconocido_no_vuelve()
    {
        var db = "nombres-partida-" + Guid.NewGuid();
        Guid copaId;

        await using (var ctx = NewCtx(db))
        {
            var copa = Publicada("Copa UMBRAL");
            copaId = copa.PartidaId;
            new SesionPartidaRepository(ctx).Add(copa);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx)
                .GetNombresByPartidaIdsAsync(new[] { copaId, Guid.NewGuid() }, CancellationToken.None);

            Assert.Equal(copaId, Assert.Single(r).PartidaId);
        }
    }

    [Fact]
    public async Task Resuelve_tambien_partidas_terminadas()
    {
        // El caso real del slice: el historial son partidas Terminadas. El metodo no filtra
        // por estado, y si lo hiciera el historial se quedaria sin nombres.
        var db = "nombres-partida-" + Guid.NewGuid();
        var t0 = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var sesion = Publicada("Copa UMBRAL");
            partidaId = sesion.PartidaId;
            sesion.Cancelar(t0); // estado terminal: basta para probar que no hay filtro de Lobby
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx)
                .GetNombresByPartidaIdsAsync(new[] { partidaId }, CancellationToken.None);

            Assert.Equal("Copa UMBRAL", Assert.Single(r).Nombre);
        }
    }
}
