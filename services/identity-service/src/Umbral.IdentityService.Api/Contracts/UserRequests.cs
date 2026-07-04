namespace Umbral.IdentityService.Api.Contracts;

public sealed record UpdateUserGeneralDataRequest(string Name, string Email);

public sealed record ChangeUserRoleRequest(string Rol);
