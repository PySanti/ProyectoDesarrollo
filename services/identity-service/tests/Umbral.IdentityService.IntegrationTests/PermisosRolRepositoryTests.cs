using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class PermisosRolRepositoryTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"permisos-rol-integration-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task GetMatriz_devuelve_los_tres_roles_incluso_sin_filas()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repo = new PermisosRolRepository(dbContext);

        var matriz = await repo.GetMatrizAsync(CancellationToken.None);
        Assert.Equal(3, matriz.Count);
        Assert.Contains(RolUsuario.Administrador, matriz.Keys);
    }

    [Fact]
    public async Task ReplaceForRol_reemplaza_el_set_completo()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repo = new PermisosRolRepository(dbContext);

        await repo.ReplaceForRolAsync(RolUsuario.Operador,
            new[] { PermisoFuncional.GestionarPartidas, PermisoFuncional.ParticiparEnPartidas }, CancellationToken.None);
        await repo.ReplaceForRolAsync(RolUsuario.Operador,
            new[] { PermisoFuncional.GestionarEquipos }, CancellationToken.None);

        var permisos = await repo.GetByRolAsync(RolUsuario.Operador, CancellationToken.None);
        Assert.Equal(new[] { PermisoFuncional.GestionarEquipos }, permisos);
    }

    [Fact]
    public async Task ReplaceForRol_con_vacio_borra_todo()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repo = new PermisosRolRepository(dbContext);

        await repo.ReplaceForRolAsync(RolUsuario.Participante, new[] { PermisoFuncional.GestionarEquipos }, CancellationToken.None);
        await repo.ReplaceForRolAsync(RolUsuario.Participante, Array.Empty<PermisoFuncional>(), CancellationToken.None);

        Assert.Empty(await repo.GetByRolAsync(RolUsuario.Participante, CancellationToken.None));
    }
}
