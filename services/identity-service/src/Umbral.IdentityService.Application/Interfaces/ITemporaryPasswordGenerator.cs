namespace Umbral.IdentityService.Application.Interfaces;

/// <summary>
/// Genera contraseñas temporales fuertes y únicas por usuario. El texto plano resultante
/// solo debe usarse en memoria durante la creación del usuario (asignarlo en Keycloak y
/// enviarlo por correo) y nunca debe persistirse (RB-U03).
/// </summary>
public interface ITemporaryPasswordGenerator
{
    string Generate();
}
