namespace Umbral.IdentityService.Domain.Enums;

/// <summary>
/// Los privilegios que el panel de gobernanza (HU-04) puede mover entre roles.
/// <para>
/// <see cref="PermisoFuncional.ParticiparEnPartidas"/> queda fuera a proposito: existe en el dominio,
/// pero esta fijo al rol Participante como composite declarado en umbral-realm.json. Solo el rol
/// Participante tiene cliente donde jugar, asi que moverlo no habilita nada y quitarlo tumbaria
/// el gameplay del movil.
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
