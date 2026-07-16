using MediatR;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CreateUserWithInitialRoleCommandHandler : IRequestHandler<CreateUserWithInitialRoleCommand, CreateUserWithInitialRoleResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IKeycloakIdentityPort _keycloakIdentityPort;
    private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;
    private readonly IIdentityEventsPublisher _identityEventsPublisher;

    public CreateUserWithInitialRoleCommandHandler(
        IUsuarioRepository usuarioRepository,
        IKeycloakIdentityPort keycloakIdentityPort,
        ITemporaryPasswordGenerator temporaryPasswordGenerator,
        IIdentityEventsPublisher identityEventsPublisher)
    {
        _usuarioRepository = usuarioRepository;
        _keycloakIdentityPort = keycloakIdentityPort;
        _temporaryPasswordGenerator = temporaryPasswordGenerator;
        _identityEventsPublisher = identityEventsPublisher;
    }

    public async Task<CreateUserWithInitialRoleResponse> Handle(CreateUserWithInitialRoleCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _usuarioRepository.ExistsByEmailAsync(normalizedEmail, null, cancellationToken))
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        // La contraseña temporal es única por usuario y solo vive en memoria durante el request:
        // se asigna en Keycloak y se envía por correo, pero nunca se persiste (RB-U03).
        var temporaryPassword = _temporaryPasswordGenerator.Generate();

        var keycloakId = await _keycloakIdentityPort.CreateUserWithInitialRoleAsync(
            request.Name,
            normalizedEmail,
            request.InitialRole,
            temporaryPassword,
            cancellationToken);

        var role = request.InitialRole switch
        {
            "Administrador" => RolUsuario.Administrador,
            "Operador" => RolUsuario.Operador,
            _ => RolUsuario.Participante
        };

        var usuario = Usuario.Crear(keycloakId, request.Name, normalizedEmail, role);

        try
        {
            await _usuarioRepository.AddAsync(usuario, cancellationToken);
        }
        catch
        {
            // Persistencia local falló tras crear en Keycloak: deshacer Keycloak para no dejar huérfanos.
            await CompensateKeycloakAsync(keycloakId, cancellationToken);
            throw;
        }

        // El usuario ya está creado (Keycloak + local): el correo de bienvenida se publica como
        // evento async (RabbitMQ) y es best-effort (ADR-0012) — un fallo de publicación se loguea
        // pero NO compensa la creación, que ya es un hecho consumado.
        await _identityEventsPublisher.PublishCredencialTemporalEmitidaAsync(
            new CredencialTemporalEmitidaIntegrationEvent(usuario.Nombre, usuario.Correo, usuario.Rol.ToString(), temporaryPassword, DateTime.UtcNow),
            cancellationToken);

        return new CreateUserWithInitialRoleResponse(
            usuario.UsuarioId.Valor,
            usuario.KeycloakId,
            usuario.Nombre,
            usuario.Correo,
            usuario.Rol.ToString(),
            usuario.Estado.ToString());
    }

    private async Task CompensateKeycloakAsync(string keycloakId, CancellationToken cancellationToken)
    {
        try
        {
            await _keycloakIdentityPort.DeleteUserAsync(keycloakId, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: no enmascarar la excepción original de la operación.
        }
    }
}
