using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IPermisosRolRepository
{
    Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken cancellationToken);
    Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken cancellationToken);
}
