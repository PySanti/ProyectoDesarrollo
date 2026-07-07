using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetPermisosRolesQueryHandler : IRequestHandler<GetPermisosRolesQuery, PermisosRolesResponse>
{
    private readonly IPermisosRolRepository _permisosRol;

    public GetPermisosRolesQueryHandler(IPermisosRolRepository permisosRol) => _permisosRol = permisosRol;

    public async Task<PermisosRolesResponse> Handle(GetPermisosRolesQuery request, CancellationToken cancellationToken)
    {
        var matriz = await _permisosRol.GetMatrizAsync(cancellationToken);
        var roles = matriz
            .OrderBy(kv => kv.Key)
            .Select(kv => new RolPermisosDto(
                kv.Key.ToString(),
                kv.Value.Select(p => p.ToString()).ToList(),
                PrivilegiosGobernanza: kv.Key == RolUsuario.Administrador))
            .ToList();
        return new PermisosRolesResponse(roles);
    }
}
