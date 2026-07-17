namespace Umbral.IdentityService.Application.DTOs;

public sealed record HistorialNombresEquipoResponse(
    IReadOnlyList<HistorialNombreEquipoItem> Historial);

public sealed record HistorialNombreEquipoItem(
    string NombreEquipo, Guid EquipoId, DateTime FechaRegistro);
