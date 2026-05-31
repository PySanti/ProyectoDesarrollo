namespace Umbral.TriviaGame.Application.Ports;

/// <summary>
/// Puerto para obtener información del usuario autenticado en el contexto actual.
/// La implementación concreta (Infrastructure/Api) extrae los datos del JWT o del HttpContext.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Identificador del operador autenticado (Keycloak subject / claim).
    /// </summary>
    string OperatorId { get; }
}
