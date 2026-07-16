using Umbral.IdentityService.Domain.ValueObjects;
using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CambiarRolUsuarioCommandHandler : IRequestHandler<CambiarRolUsuarioCommand, CambiarRolUsuarioResponse>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEquipoRepository _equipos;
    private readonly IKeycloakIdentityPort _keycloak;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public CambiarRolUsuarioCommandHandler(
        IUsuarioRepository usuarios,
        IEquipoRepository equipos,
        IKeycloakIdentityPort keycloak,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _usuarios = usuarios;
        _equipos = equipos;
        _keycloak = keycloak;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<CambiarRolUsuarioResponse> Handle(CambiarRolUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(UsuarioLocalId.From(request.UserId), cancellationToken)
            ?? throw new UserNotFoundException(request.UserId);

        var rolAnterior = usuario.Rol;
        var rolNuevo = Enum.Parse<RolUsuario>(request.Rol);

        if (rolAnterior == rolNuevo)
        {
            return new CambiarRolUsuarioResponse(usuario.UsuarioId.Valor, rolAnterior.ToString());
        }

        // Guard de dominio (admin inmutable, spec 5.3 paso 3) ANTES del check de equipo y de Keycloak.
        // Para un admin, CambiarRol lanza sin mutar; para el resto la mutación real ocurre tras Keycloak.
        if (rolAnterior == RolUsuario.Administrador)
        {
            usuario.CambiarRol(rolNuevo); // lanza RolDeAdministradorInmutableException
        }

        // La membresía de equipos está keyeada por el sub de Keycloak (Guid).
        if (Guid.TryParse(usuario.KeycloakId, out var keycloakGuid) &&
            await _equipos.ExistsActiveTeamByUserIdAsync(keycloakGuid, cancellationToken))
        {
            throw new UsuarioConEquipoActivoException(usuario.UsuarioId.Valor);
        }

        await _keycloak.ChangeUserRealmRoleAsync(usuario.KeycloakId, rolAnterior.ToString(), rolNuevo.ToString(), cancellationToken);

        usuario.CambiarRol(rolNuevo);
        await _usuarios.UpdateAsync(usuario, cancellationToken);

        await _events.PublishRolUsuarioModificadoAsync(
            new RolUsuarioModificadoIntegrationEvent(usuario.UsuarioId.Valor, rolAnterior.ToString(), rolNuevo.ToString(),
                _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        return new CambiarRolUsuarioResponse(usuario.UsuarioId.Valor, rolNuevo.ToString());
    }
}
