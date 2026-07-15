using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class HistorialNombreEquipoPersistenceTests
{
    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"hist-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task AddRange_y_GetByUsuario_devuelve_orden_ascendente_por_fecha()
    {
        var usuario = Guid.NewGuid();
        var equipo = Guid.NewGuid();
        await using var ctx = NewContext();
        var repo = new HistorialNombreEquipoRepository(ctx);

        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.AddRangeAsync(new[]
        {
            HistorialNombreEquipo.Registrar(usuario, equipo, "Segundo", t0.AddDays(1)),
            HistorialNombreEquipo.Registrar(usuario, equipo, "Primero", t0),
        }, CancellationToken.None);

        var result = await repo.GetByUsuarioAsync(usuario, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Primero", result[0].NombreEquipo);
        Assert.Equal("Segundo", result[1].NombreEquipo);
    }

    [Fact]
    public async Task GetByUsuario_sin_registros_devuelve_lista_vacia()
    {
        await using var ctx = NewContext();
        var repo = new HistorialNombreEquipoRepository(ctx);
        var result = await repo.GetByUsuarioAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Empty(result);
    }
}
