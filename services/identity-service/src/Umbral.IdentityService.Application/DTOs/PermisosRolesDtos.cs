namespace Umbral.IdentityService.Application.DTOs;

public sealed record RolPermisosDto(string Rol, IReadOnlyList<string> Permisos, bool PrivilegiosGobernanza);

public sealed record PermisosRolesResponse(IReadOnlyList<RolPermisosDto> Roles);
