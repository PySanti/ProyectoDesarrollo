using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class Usuario
{
    public Guid UsuarioId { get; private set; }
    public string KeycloakId { get; private set; }
    public string Nombre { get; private set; }
    public string Correo { get; private set; }
    public RolUsuario Rol { get; private set; }
    public EstadoUsuario Estado { get; private set; }

    private Usuario(Guid usuarioId, string keycloakId, string nombre, string correo, RolUsuario rol)
    {
        UsuarioId = usuarioId;
        KeycloakId = keycloakId;
        Nombre = nombre;
        Correo = correo;
        Rol = rol;
        Estado = EstadoUsuario.Activo;
    }

    public static Usuario Crear(string keycloakId, string nombre, string correo, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(keycloakId)) throw new ArgumentException("KeycloakId requerido", nameof(keycloakId));
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido", nameof(nombre));
        if (string.IsNullOrWhiteSpace(correo)) throw new ArgumentException("Correo requerido", nameof(correo));

        return new Usuario(Guid.NewGuid(), keycloakId.Trim(), nombre.Trim(), correo.Trim().ToLowerInvariant(), rol);
    }
}
