using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.IntegrationTests;

public class PurgaEventosProcesadosTests
{
    [Fact]
    public async Task Elimina_solo_los_eventos_procesados_antes_del_limite()
    {
        var opciones = new DbContextOptionsBuilder<PuntuacionesDbContext>()
            .UseInMemoryDatabase($"purga-{Guid.NewGuid()}").Options;
        var limite = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
        var viejoId = Guid.NewGuid();
        var recienteId = Guid.NewGuid();

        await using (var db = new PuntuacionesDbContext(opciones))
        {
            db.EventosProcesados.Add(EventoProcesado.Registrar(viejoId, "PartidaIniciada", limite.AddDays(-10), limite.AddDays(-10)));
            db.EventosProcesados.Add(EventoProcesado.Registrar(recienteId, "PartidaIniciada", limite.AddDays(1), limite.AddDays(1)));
            await db.SaveChangesAsync();
        }

        await using (var db = new PuntuacionesDbContext(opciones))
        {
            var repo = new ProyeccionesRepository(db);
            var eliminados = await repo.EliminarEventosProcesadosAnterioresAsync(limite, CancellationToken.None);
            await db.SaveChangesAsync();
            Assert.Equal(1, eliminados);
        }

        await using var lectura = new PuntuacionesDbContext(opciones);
        var restante = Assert.Single(await lectura.EventosProcesados.ToListAsync());
        Assert.Equal(recienteId, restante.EventId);
    }
}
