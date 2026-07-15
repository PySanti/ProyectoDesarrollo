namespace Umbral.IdentityService.Application.Interfaces;

/// <summary>
/// Resultado de un intento best-effort de notificar a los integrantes de un equipo.
/// Permite informar al operador cuántos fueron notificados y si el servidor de correo
/// respondió (para distinguir "no respondió" de "no había a quién notificar").
/// </summary>
/// <param name="Total">Integrantes a los que se intentó notificar.</param>
/// <param name="Notificados">Correos que el servidor aceptó.</param>
/// <param name="FallasServidor">Envíos en los que el servidor de correo no respondió (timeout/SMTP caído).</param>
public sealed record TeamNotificationOutcome(int Total, int Notificados, int FallasServidor)
{
    /// <summary>El servidor respondió si ningún envío falló por falta de respuesta del servidor.</summary>
    public bool ServidorRespondio => FallasServidor == 0;
}
