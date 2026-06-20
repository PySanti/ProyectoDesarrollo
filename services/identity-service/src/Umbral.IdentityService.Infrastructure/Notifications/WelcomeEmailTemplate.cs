using System.Net;
using Umbral.IdentityService.Application.Abstractions.Notifications;

namespace Umbral.IdentityService.Infrastructure.Notifications;

/// <summary>
/// Construye el correo de bienvenida con los estilos de la plataforma UMBRAL (paleta y
/// tipografías de DESIGN.md). Usa HTML table-based con estilos inline porque la mayoría de
/// clientes de correo ignoran &lt;style&gt; y fuentes externas; se declaran fallbacks de sistema.
/// </summary>
public static class WelcomeEmailTemplate
{
    public const string Subject = "Tu acceso a UMBRAL";

    // Tokens de marca (DESIGN.md).
    private const string Bg = "#f7f4f7";
    private const string Surface = "#ffffff";
    private const string Ink = "#1b131a";
    private const string InkSoft = "#433942";
    private const string Muted = "#6e666d";
    private const string Line = "#e1dce0";
    private const string Primary = "#b545ae";
    private const string PrimaryFill = "#982f93";
    private const string PrimaryStrong = "#7d2278";
    private const string PrimaryWash = "#fbe8f8";

    private const string DisplayFont = "'Space Grotesk', 'Segoe UI', system-ui, Arial, sans-serif";
    private const string BodyFont = "'Inter', 'Segoe UI', system-ui, Arial, sans-serif";
    private const string MonoFont = "'JetBrains Mono', 'Courier New', ui-monospace, monospace";

    public static string BuildHtml(UserWelcomeEmailMessage message)
    {
        var name = WebUtility.HtmlEncode(message.Name);
        var email = WebUtility.HtmlEncode(message.Email);
        var role = WebUtility.HtmlEncode(message.Role);
        var password = WebUtility.HtmlEncode(message.TemporaryPassword);

        return $"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{Subject}</title>
        </head>
        <body style="margin:0;padding:0;background-color:{Bg};">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{Bg};padding:32px 12px;">
            <tr>
              <td align="center">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background-color:{Surface};border:1px solid {Line};border-radius:16px;overflow:hidden;">
                  <tr>
                    <td style="background:linear-gradient(135deg,{Primary} 0%,{PrimaryStrong} 100%);padding:28px 32px;">
                      <span style="font-family:{DisplayFont};font-size:26px;font-weight:700;letter-spacing:-0.01em;color:#ffffff;">UMBRAL</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:32px;">
                      <h1 style="margin:0 0 12px 0;font-family:{DisplayFont};font-size:22px;font-weight:600;color:{Ink};">Hola, {name}</h1>
                      <p style="margin:0 0 20px 0;font-family:{BodyFont};font-size:15px;line-height:1.6;color:{InkSoft};">
                        Se ha creado una cuenta para ti en la plataforma <strong>UMBRAL</strong> con el rol
                        <strong style="color:{PrimaryFill};">{role}</strong>. Usa estas credenciales para iniciar sesión por primera vez.
                      </p>

                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{PrimaryWash};border:1px solid {Line};border-radius:12px;margin:0 0 20px 0;">
                        <tr>
                          <td style="padding:20px 24px;">
                            <p style="margin:0 0 6px 0;font-family:{BodyFont};font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:0.04em;color:{Muted};">Usuario</p>
                            <p style="margin:0 0 16px 0;font-family:{MonoFont};font-size:15px;color:{Ink};word-break:break-all;">{email}</p>
                            <p style="margin:0 0 6px 0;font-family:{BodyFont};font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:0.04em;color:{Muted};">Contraseña temporal</p>
                            <p style="margin:0;font-family:{MonoFont};font-size:20px;font-weight:700;letter-spacing:0.06em;color:{PrimaryStrong};word-break:break-all;">{password}</p>
                          </td>
                        </tr>
                      </table>

                      <p style="margin:0 0 8px 0;font-family:{BodyFont};font-size:14px;line-height:1.6;color:{InkSoft};">
                        Por tu seguridad, esta contraseña es <strong>temporal</strong>: el sistema te pedirá cambiarla
                        la primera vez que inicies sesión.
                      </p>
                      <p style="margin:0;font-family:{BodyFont};font-size:13px;line-height:1.6;color:{Muted};">
                        Si no esperabas este correo, puedes ignorarlo.
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:18px 32px;border-top:1px solid {Line};">
                      <p style="margin:0;font-family:{BodyFont};font-size:12px;color:{Muted};">UMBRAL · Trivia y Búsqueda del Tesoro en tiempo real</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

    public static string BuildPlainText(UserWelcomeEmailMessage message) =>
        $"""
        UMBRAL

        Hola, {message.Name}

        Se ha creado una cuenta para ti en la plataforma UMBRAL con el rol {message.Role}.
        Usa estas credenciales para iniciar sesión por primera vez:

        Usuario: {message.Email}
        Contraseña temporal: {message.TemporaryPassword}

        Por tu seguridad, esta contraseña es temporal: el sistema te pedirá cambiarla la
        primera vez que inicies sesión.

        Si no esperabas este correo, puedes ignorarlo.

        UMBRAL · Trivia y Búsqueda del Tesoro en tiempo real
        """;
}
