namespace Umbral.IdentityService.Api.Contracts;

public sealed record CrearEquipoAdminRequest(string NombreEquipo, Guid LiderUserId);
public sealed record RenombrarEquipoRequest(string NombreEquipo);
public sealed record ReasignarLiderazgoAdminRequest(Guid NuevoLiderUserId);
public sealed record CambiarEstadoEquipoRequest(string Estado);
