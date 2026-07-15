using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class HistorialBackfillTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"backfill-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Backfill_inserta_una_fila_por_integrante_de_equipo_activo()
    {
        await using var ctx = NewContext();
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Cascada", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        ctx.Equipos.Add(equipo);
        await ctx.SaveChangesAsync();

        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);

        Assert.Equal(2, await ctx.HistorialNombresEquipo.CountAsync());
    }

    [Fact]
    public async Task Backfill_es_idempotente()
    {
        await using var ctx = NewContext();
        var equipo = Equipo.CrearPorParticipante("Cascada", Guid.NewGuid());
        ctx.Equipos.Add(equipo);
        await ctx.SaveChangesAsync();

        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);
        await HistorialBackfill.EjecutarAsync(ctx, TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, await ctx.HistorialNombresEquipo.CountAsync());
    }
}
