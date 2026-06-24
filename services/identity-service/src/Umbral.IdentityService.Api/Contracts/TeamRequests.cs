namespace Umbral.IdentityService.Api.Contracts;

public sealed record CrearEquipoRequest(string NombreEquipo);
public sealed record TransferirLiderazgoRequest(Guid NuevoLiderUserId);
public sealed record EnviarInvitacionRequest(Guid InvitadoUserId);
