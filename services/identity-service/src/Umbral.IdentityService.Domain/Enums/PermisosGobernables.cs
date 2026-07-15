namespace Umbral.IdentityService.Domain.Enums;

/// <summary>
/// Los privilegios que el panel de gobernanza (HU-04) puede mover entre roles.
/// <para>
/// <see cref="PermisoFuncional.ParticiparEnPartidas"/> queda fuera a propósito: existe en el dominio,
/// pero está fijo al rol Participante como composite declarado en umbral-realm.json. Solo el rol
/// Participante tiene cliente donde jugar, así que moverlo no habilita nada y quitarlo tumbaría
/// el gameplay del móvil.
/// </para>
/// </summary>
public static class PermisosGobernables
{
    public static readonly IReadOnlySet<PermisoFuncional> Todos = new HashSet<PermisoFuncional>
    {
        PermisoFuncional.GestionarPartidas,
        PermisoFuncional.GestionarEquipos
    };
}
