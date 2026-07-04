using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class PermisosRolRepository : IPermisosRolRepository
{
    private readonly IdentityDbContext _dbContext;

    public PermisosRolRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken cancellationToken)
    {
        var filas = await _dbContext.PermisosRol.AsNoTracking().ToListAsync(cancellationToken);
        return Enum.GetValues<RolUsuario>().ToDictionary(
            rol => rol,
            rol => (IReadOnlyList<PermisoFuncional>)filas.Where(f => f.Rol == rol).Select(f => f.Permiso).OrderBy(p => p).ToList());
    }

    public async Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken cancellationToken)
        => await _dbContext.PermisosRol.AsNoTracking()
            .Where(f => f.Rol == rol).Select(f => f.Permiso).OrderBy(p => p).ToListAsync(cancellationToken);

    public async Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken cancellationToken)
    {
        var actuales = await _dbContext.PermisosRol.Where(f => f.Rol == rol).ToListAsync(cancellationToken);
        _dbContext.PermisosRol.RemoveRange(actuales);
        _dbContext.PermisosRol.AddRange(permisos.Distinct().Select(p => new PermisoRol(rol, p)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
