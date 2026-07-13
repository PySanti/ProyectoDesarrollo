namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

public static class IdentityEventRouting
{
    // Mapa explícito (sin kebab algorítmico): el contrato documenta esta tabla 1:1.
    private static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>
    {
        ["EquipoCreado"] = "identity.equipo-creado.v1",
        ["InvitacionEquipoCreada"] = "identity.invitacion-equipo-creada.v1",
        ["InvitacionEquipoAceptada"] = "identity.invitacion-equipo-aceptada.v1",
        ["InvitacionEquipoRechazada"] = "identity.invitacion-equipo-rechazada.v1",
        ["RolUsuarioModificado"] = "identity.rol-usuario-modificado.v1",
        ["PermisosRolActualizados"] = "identity.permisos-rol-actualizados.v1",
        ["EquipoEliminado"] = "identity.equipo-eliminado.v1",
        ["LiderazgoEquipoModificado"] = "identity.liderazgo-equipo-modificado.v1",
        ["EquipoDesactivado"] = "identity.equipo-desactivado.v1",
        ["EquipoReactivado"] = "identity.equipo-reactivado.v1",
        ["CredencialTemporalEmitida"] = "identity.credencial-temporal-emitida.v1",
    };

    public static string RoutingKeyFor(string eventType) => Keys[eventType];
}
