using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class PermisoRol
{
    public RolUsuario Rol { get; private set; }
    public PermisoFuncional Permiso { get; private set; }

    private PermisoRol() { }

    public PermisoRol(RolUsuario rol, PermisoFuncional permiso)
    {
        Rol = rol;
        Permiso = permiso;
    }
}
