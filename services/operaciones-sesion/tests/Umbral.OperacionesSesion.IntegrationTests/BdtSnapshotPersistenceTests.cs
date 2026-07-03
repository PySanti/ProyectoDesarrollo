using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class BdtSnapshotPersistenceTests
{
    private static DbContextOptions<OperacionesSesionDbContext> InMemoryOptions() =>
        new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase($"bdt-{Guid.NewGuid()}").Options;

    [Fact]
    public async Task Roundtrip_persists_bdt_stages_and_treasures()
    {
        var options = InMemoryOptions();
        var partidaId = Guid.NewGuid();

        var etapas = new List<EtapaSnapshot>
        {
            new(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new(Guid.NewGuid(), 2, "QR-2", 70, 90),
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var reloaded = await ctx.Sesiones
                .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
                .FirstAsync(s => s.PartidaId == partidaId);
            var j = reloaded.Juegos.Single();
            Assert.Equal("Plaza", j.AreaBusqueda);
            Assert.Equal(2, j.Etapas.Count);
            Assert.Equal("QR-1", j.Etapas.OrderBy(e => e.Orden).First().CodigoQREsperado);
        }
    }
}
