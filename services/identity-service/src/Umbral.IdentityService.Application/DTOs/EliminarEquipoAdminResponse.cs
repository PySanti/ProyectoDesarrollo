namespace Umbral.IdentityService.Application.DTOs;

/// <summary>
/// Resultado de eliminar un equipo desde administración, incluyendo el desenlace de la
/// notificación best-effort a los integrantes para informarlo en la interfaz.
/// </summary>
public sealed record EliminarEquipoAdminResponse(
    Guid EquipoId,
    string NombreEquipo,
    int IntegrantesTotal,
    int IntegrantesNotificados,
    bool ServidorCorreoRespondio);
