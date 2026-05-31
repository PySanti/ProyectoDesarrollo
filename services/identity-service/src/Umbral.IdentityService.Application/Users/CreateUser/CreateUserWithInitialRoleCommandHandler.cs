using MediatR;
using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Users.CreateUser;

public sealed class CreateUserWithInitialRoleCommandHandler : IRequestHandler<CreateUserWithInitialRoleCommand, CreateUserWithInitialRoleResponse>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IKeycloakIdentityPort _keycloakIdentityPort;

    public CreateUserWithInitialRoleCommandHandler(
        IUsuarioRepository usuarioRepository,
        IKeycloakIdentityPort keycloakIdentityPort)
    {
        _usuarioRepository = usuarioRepository;
        _keycloakIdentityPort = keycloakIdentityPort;
    }

    public async Task<CreateUserWithInitialRoleResponse> Handle(CreateUserWithInitialRoleCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _usuarioRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        var keycloakId = await _keycloakIdentityPort.CreateUserWithInitialRoleAsync(
            request.Name,
            normalizedEmail,
            request.InitialRole,
            cancellationToken);

        var role = request.InitialRole switch
        {
            "Administrador" => RolUsuario.Administrador,
            "Operador" => RolUsuario.Operador,
            _ => RolUsuario.Participante
        };

        var usuario = Usuario.Crear(keycloakId, request.Name, normalizedEmail, role);
        await _usuarioRepository.AddAsync(usuario, cancellationToken);

        return new CreateUserWithInitialRoleResponse(
            usuario.UsuarioId,
            usuario.KeycloakId,
            usuario.Nombre,
            usuario.Correo,
            usuario.Rol.ToString(),
            usuario.Estado.ToString());
    }
}
