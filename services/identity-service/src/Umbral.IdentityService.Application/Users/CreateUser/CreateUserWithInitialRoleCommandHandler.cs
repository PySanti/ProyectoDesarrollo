using MediatR;
using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Abstractions.Notifications;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Abstractions.Security;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Users.CreateUser;

public sealed class CreateUserWithInitialRoleCommandHandler : IRequestHandler<CreateUserWithInitialRoleCommand, CreateUserWithInitialRoleResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IKeycloakIdentityPort _keycloakIdentityPort;
    private readonly ITemporaryPasswordGenerator _temporaryPasswordGenerator;
    private readonly IUserWelcomeEmailSender _welcomeEmailSender;

    public CreateUserWithInitialRoleCommandHandler(
        IUsuarioRepository usuarioRepository,
        IKeycloakIdentityPort keycloakIdentityPort,
        ITemporaryPasswordGenerator temporaryPasswordGenerator,
        IUserWelcomeEmailSender welcomeEmailSender)
    {
        _usuarioRepository = usuarioRepository;
        _keycloakIdentityPort = keycloakIdentityPort;
        _temporaryPasswordGenerator = temporaryPasswordGenerator;
        _welcomeEmailSender = welcomeEmailSender;
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

        try
        {
            // El envío es el último paso: si falla, la operación falla y se compensan ambas creaciones
            // (all-or-nothing), de modo que no queden usuarios sin notificar.
            await _welcomeEmailSender.SendWelcomeEmailAsync(
                new UserWelcomeEmailMessage(usuario.Nombre, usuario.Correo, usuario.Rol.ToString(), temporaryPassword),
                cancellationToken);
        }
        catch
        {
            await CompensateLocalAsync(usuario, cancellationToken);
            await CompensateKeycloakAsync(keycloakId, cancellationToken);
            throw;
        }

        return new CreateUserWithInitialRoleResponse(
            usuario.UsuarioId,
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

    private async Task CompensateLocalAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            await _usuarioRepository.RemoveAsync(usuario, cancellationToken);
        }
        catch
        {
            // Compensación best-effort: no enmascarar la excepción original de la operación.
        }
    }
}
