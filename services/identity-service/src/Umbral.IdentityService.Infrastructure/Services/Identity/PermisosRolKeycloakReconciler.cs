using Microsoft.Extensions.Logging;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Services.Identity;

/// <summary>
/// Converge los composites de Keycloak hacia la matriz <c>permisos_rol</c> de la DB al arrancar.
/// <para>
/// El realm declara lo fijo (Participante -> ParticiparEnPartidas) y la DB gobierna lo variable (los
/// dos privilegios del panel). Como no se solapan, keycloak-config puede reaplicar el realm sin pisar
/// la gobernanza. Este reconciliador es quien lleva la matriz de permisos_rol a Keycloak al arrancar.
/// </para>
/// <para>
/// Reemplaza la decisión "sin reconciliación al arranque" del diseño SP-5b §9, que solo contempló
/// la deriva puntual tras un 502 y es anterior a <c>keycloak-config</c>.
/// </para>
/// </summary>
public sealed class PermisosRolKeycloakReconciler
{
    private readonly IPermisosRolRepository _permisosRol;
    private readonly IKeycloakIdentityPort _keycloak;
    private readonly ILogger<PermisosRolKeycloakReconciler> _logger;

    public PermisosRolKeycloakReconciler(
        IPermisosRolRepository permisosRol,
        IKeycloakIdentityPort keycloak,
        ILogger<PermisosRolKeycloakReconciler> logger)
    {
        _permisosRol = permisosRol;
        _keycloak = keycloak;
        _logger = logger;
    }

    /// <summary>
    /// Aplica la matriz de la DB sobre Keycloak. Best-effort: un fallo de Keycloak se registra y no
    /// tumba el arranque — el servicio sigue sirviendo y la deriva se repara en el siguiente arranque
    /// o al reenviar el PUT de gobernanza.
    /// </summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var matriz = await _permisosRol.GetMatrizAsync(cancellationToken);

            foreach (var rol in Enum.GetValues<RolUsuario>())
            {
                var deseados = matriz.TryGetValue(rol, out var permisos)
                    ? permisos.ToHashSet()
                    : new HashSet<PermisoFuncional>();

                // Add/Remove de Keycloak son idempotentes (y Remove tolera el 404), así que se
                // afirma el estado completo sin leer antes: 3 roles x 2 permisos = 6 llamadas.
                // Solo lo gobernable: ParticiparEnPartidas es un composite fijo del realm y borrarlo
                // dejaría al Participante sin poder jugar.
                foreach (var permiso in PermisosGobernables.Todos)
                {
                    if (deseados.Contains(permiso))
                        await _keycloak.AddCompositeToRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
                    else
                        await _keycloak.RemoveCompositeFromRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
                }

                _logger.LogInformation(
                    "Permisos de {Rol} reconciliados en Keycloak: [{Permisos}]",
                    rol,
                    string.Join(", ", deseados.OrderBy(p => p)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudieron reconciliar los permisos por rol contra Keycloak. Los tokens pueden "
                + "no reflejar la matriz de permisos_rol hasta el próximo arranque o un PUT de gobernanza.");
        }
    }
}
