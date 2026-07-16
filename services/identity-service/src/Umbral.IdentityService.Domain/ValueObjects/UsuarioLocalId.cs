namespace Umbral.IdentityService.Domain.ValueObjects;

/// <summary>
/// El id local del usuario, generado por UMBRAL. No confundir con el sub de OIDC
/// (<c>Usuario.KeycloakId</c>), que es el id con el que el actor llega en el token y con el que se
/// indexa el mundo de equipos (<c>ParticipanteEquipo.SubjectId</c>). Son dos Guid sin relacion
/// entre si: este tipo existe para que mezclarlos no compile.
/// </summary>
public readonly record struct UsuarioLocalId(Guid Valor)
{
    public static UsuarioLocalId New() => new(Guid.NewGuid());
    public static UsuarioLocalId From(Guid valor) => new(valor);
}
