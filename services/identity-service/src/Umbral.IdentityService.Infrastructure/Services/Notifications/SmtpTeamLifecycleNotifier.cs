using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Services.Notifications;

/// <summary>
/// Envía correos de ciclo de vida de equipo vía SMTP usando <see cref="SmtpClient"/> (BCL),
/// siguiendo el mismo patrón de conexión que <see cref="SmtpUserWelcomeEmailSender"/>. A
/// diferencia de aquel, esta implementación es <b>best-effort</b> (ADR-0012): un destinatario
/// no resoluble o cualquier fallo de envío se registra en el log y nunca se relanza, porque
/// estas notificaciones no deben revertir ni bloquear la operación de dominio que las origina.
/// </summary>
public sealed class SmtpTeamLifecycleNotifier : ITeamLifecycleNotifier
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpTeamLifecycleNotifier> _logger;

    public SmtpTeamLifecycleNotifier(
        IUsuarioRepository usuarioRepository,
        IOptions<SmtpOptions> options,
        ILogger<SmtpTeamLifecycleNotifier> logger)
    {
        _usuarioRepository = usuarioRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct)
    {
        var (subject, plainText) = TeamLifecycleEmailTemplate.BuildEquipoEliminado(nombreEquipo);

        foreach (var miembroId in miembros)
        {
            await EnviarCorreoBestEffortAsync(miembroId, subject, plainText, ct);
        }
    }

    public async Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct)
    {
        var (subjectAnterior, textoAnterior) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: false);
        var (subjectNuevo, textoNuevo) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: true);

        await EnviarCorreoBestEffortAsync(liderAnteriorUserId, subjectAnterior, textoAnterior, ct);
        await EnviarCorreoBestEffortAsync(nuevoLiderUserId, subjectNuevo, textoNuevo, ct);
    }

    private async Task EnviarCorreoBestEffortAsync(Guid usuarioId, string subject, string plainText, CancellationToken ct)
    {
        try
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, ct);
            if (usuario is null)
            {
                _logger.LogWarning(
                    "No se pudo notificar el ciclo de vida del equipo: usuario {UsuarioId} no existe.",
                    usuarioId);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                _logger.LogWarning(
                    "No se pudo notificar el ciclo de vida del equipo a {UsuarioId}: configuración SMTP incompleta.",
                    usuarioId);
                return;
            }

            await EnviarAsync(usuario, subject, plainText, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Fallo best-effort al notificar ciclo de vida de equipo al usuario {UsuarioId}.",
                usuarioId);
        }
    }

    private async Task EnviarAsync(Usuario usuario, string subject, string plainText, CancellationToken ct)
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
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = plainText,
            BodyEncoding = Encoding.UTF8
        };
        mail.To.Add(new MailAddress(usuario.Correo, usuario.Nombre, Encoding.UTF8));

        await smtp.SendMailAsync(mail, ct);
    }
}
