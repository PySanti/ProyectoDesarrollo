using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class ActualizarPermisosRolCommandHandler : IRequestHandler<ActualizarPermisosRolCommand, RolPermisosDto>
{
    private readonly IPermisosRolRepository _permisosRol;
    private readonly IKeycloakIdentityPort _keycloak;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public ActualizarPermisosRolCommandHandler(
        IPermisosRolRepository permisosRol,
        IKeycloakIdentityPort keycloak,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _permisosRol = permisosRol;
        _keycloak = keycloak;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<RolPermisosDto> Handle(ActualizarPermisosRolCommand request, CancellationToken cancellationToken)
    {
        var rol = Enum.Parse<RolUsuario>(request.Rol);
        var deseados = request.Permisos.Select(p => Enum.Parse<PermisoFuncional>(p)).Distinct().OrderBy(p => p).ToList();

        var actuales = await _permisosRol.GetByRolAsync(rol, cancellationToken);
        var agregar = deseados.Except(actuales).ToList();
        var quitar = actuales.Except(deseados).ToList();

        var permisosFinales = deseados.Select(p => p.ToString()).ToList();
        if (agregar.Count == 0 && quitar.Count == 0)
        {
            return new RolPermisosDto(rol.ToString(), permisosFinales, rol == RolUsuario.Administrador);
        }

        // Keycloak primero (E2): si falla, nada persiste; el PUT re-ejecutado repara.
        foreach (var permiso in agregar)
        {
            await _keycloak.AddCompositeToRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
        }
        foreach (var permiso in quitar)
        {
            await _keycloak.RemoveCompositeFromRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
        }

        await _permisosRol.ReplaceForRolAsync(rol, deseados, cancellationToken);

        await _events.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent(rol.ToString(), permisosFinales, _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        return new RolPermisosDto(rol.ToString(), permisosFinales, rol == RolUsuario.Administrador);
    }
}
