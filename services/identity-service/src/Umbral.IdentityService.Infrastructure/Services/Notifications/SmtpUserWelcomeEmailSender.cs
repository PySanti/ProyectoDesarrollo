using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Application.Exceptions;

namespace Umbral.IdentityService.Infrastructure.Services.Notifications;

/// <summary>
/// Envía el correo de bienvenida vía SMTP usando <see cref="SmtpClient"/> (BCL, sin dependencias
/// externas). Compatible con Gmail (smtp.gmail.com:587 + STARTTLS y app password). Cualquier
/// fallo se traduce a <see cref="EmailDeliveryException"/> para que el handler compense la creación.
/// </summary>
public sealed class SmtpUserWelcomeEmailSender : IUserWelcomeEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpUserWelcomeEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new EmailDeliveryException("SMTP settings are missing or incomplete (Host and FromAddress are required).");
        }

        try
        {
            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseStartTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = string.IsNullOrWhiteSpace(_options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.Username, _options.Password)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
                Subject = WelcomeEmailTemplate.Subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            mail.To.Add(new MailAddress(message.Email, message.Name, Encoding.UTF8));

            // Texto plano primero y HTML después: los clientes muestran la última vista que soportan.
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                WelcomeEmailTemplate.BuildPlainText(message), Encoding.UTF8, MediaTypeNames.Text.Plain));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                WelcomeEmailTemplate.BuildHtml(message), Encoding.UTF8, MediaTypeNames.Text.Html));

            await smtp.SendMailAsync(mail, cancellationToken);
        }
        catch (EmailDeliveryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EmailDeliveryException($"Failed to send welcome email to {message.Email}.", ex);
        }
    }
}
