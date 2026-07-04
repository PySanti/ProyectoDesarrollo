using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

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

    public void EditarDatosGenerales(string nombre, string correo)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido", nameof(nombre));
        if (string.IsNullOrWhiteSpace(correo)) throw new ArgumentException("Correo requerido", nameof(correo));

        Nombre = nombre.Trim();
        Correo = correo.Trim().ToLowerInvariant();
    }

    public void CambiarRol(RolUsuario nuevoRol)
    {
        if (Rol == RolUsuario.Administrador)
        {
            throw new RolDeAdministradorInmutableException();
        }

        Rol = nuevoRol;
    }

    public void Desactivar()
    {
        Estado = EstadoUsuario.Desactivado;
    }
}
