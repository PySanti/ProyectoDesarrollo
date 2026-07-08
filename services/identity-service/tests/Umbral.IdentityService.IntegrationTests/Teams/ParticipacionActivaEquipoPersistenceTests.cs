using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class ParticipacionActivaEquipoPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"pae-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Upsert_es_idempotente_y_Exists_lo_detecta()
    {
        await using var ctx = NewContext();
        var repo = new ParticipacionActivaEquipoRepository(ctx);
        var equipo = Guid.NewGuid();
        var partida = Guid.NewGuid();

        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);
        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);

        Assert.True(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
        Assert.Equal(1, await ctx.ParticipacionesActivasEquipo.CountAsync());
    }

    [Fact]
    public async Task RemoveByPartida_borra_y_Exists_devuelve_false()
    {
        await using var ctx = NewContext();
        var repo = new ParticipacionActivaEquipoRepository(ctx);
        var equipo = Guid.NewGuid();
        var partida = Guid.NewGuid();
        await repo.UpsertAsync(equipo, partida, DateTime.UtcNow, CancellationToken.None);

        await repo.RemoveByPartidaAsync(partida, CancellationToken.None);

        Assert.False(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
    }
}
