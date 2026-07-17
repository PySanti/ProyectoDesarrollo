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
/// <para>
/// Lo invoca el consumidor de RabbitMQ (<c>CredencialesTemporalesConsumer</c>) al autoconsumir
/// <c>EquipoEliminado</c> / <c>LiderazgoEquipoModificado</c>, nunca un handler dentro del request:
/// eliminar un equipo o reasignar liderazgo no espera al SMTP.
/// </para>
/// </summary>
public sealed class SmtpTeamLifecycleNotifier : ITeamLifecycleNotifier
{
    // Presupuesto de envío. SmtpClient.SendMailAsync ignora SmtpClient.Timeout; sin este límite,
    // un SMTP que conecta pero no responde deja el envío colgado ~100s. ponytail: 10s fijo; mover
    // a SmtpOptions si algún entorno necesita otro valor.
    private static readonly TimeSpan NotificacionTimeout = TimeSpan.FromSeconds(10);

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

    public async Task<TeamNotificationOutcome> NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct)
    {
        var (subject, plainText) = TeamLifecycleEmailTemplate.BuildEquipoEliminado(nombreEquipo);

        var notificados = 0;
        var fallasServidor = 0;
        foreach (var miembroId in miembros)
        {
            switch (await EnviarConDeadlineAsync(miembroId, subject, plainText, ct))
            {
                case ResultadoEnvio.Enviado:
                    notificados++;
                    break;
                case ResultadoEnvio.ServidorNoRespondio:
                    fallasServidor++;
                    break;
            }
        }

        return new TeamNotificationOutcome(miembros.Count, notificados, fallasServidor);
    }

    public async Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct)
    {
        var (subjectAnterior, textoAnterior) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: false);
        var (subjectNuevo, textoNuevo) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: true);

        // Deadline por destinatario, no compartido: un SMTP lento con el líder anterior no debe
        // dejar sin correo al nuevo (HU-09 exige notificar a ambos).
        await EnviarConDeadlineAsync(liderAnteriorUserId, subjectAnterior, textoAnterior, ct);
        await EnviarConDeadlineAsync(nuevoLiderUserId, subjectNuevo, textoNuevo, ct);
    }

    private enum ResultadoEnvio
    {
        Enviado,
        ServidorNoRespondio,
        Omitido
    }

    /// <summary>
    /// Envía un correo acotado por <see cref="NotificacionTimeout"/>. El presupuesto es <b>por
    /// destinatario</b>, no compartido por la operación: un SMTP lento con el primero no debe dejar
    /// sin correo al resto (RB-E15 / HU-09 exigen notificar a todos los implicados). Puede ser por
    /// destinatario porque estas notificaciones corren en el consumidor de RabbitMQ, fuera del
    /// request de la operación de dominio.
    /// </summary>
    private async Task<ResultadoEnvio> EnviarConDeadlineAsync(Guid keycloakId, string subject, string plainText, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(NotificacionTimeout);
        return await EnviarCorreoBestEffortAsync(keycloakId, subject, plainText, deadline.Token);
    }

    private async Task<ResultadoEnvio> EnviarCorreoBestEffortAsync(Guid miembroKeycloakId, string subject, string plainText, CancellationToken ct)
    {
        try
        {
            // Los miembros de equipo llegan en espacio KeycloakId (el `sub` del JWT), no en el
            // UsuarioId local: ver Equipo.EliminarPorLider/EliminarPorAdmin y
            // ReasignarLiderazgoPorAdmin. Resolver por GetByIdAsync aquí nunca encontraría al
            // destinatario.
            var usuario = await _usuarioRepository.GetByKeycloakIdAsync(miembroKeycloakId, ct);
            if (usuario is null)
            {
                _logger.LogWarning(
                    "No se pudo notificar el ciclo de vida del equipo: usuario con KeycloakId {KeycloakId} no existe.",
                    miembroKeycloakId);
                return ResultadoEnvio.Omitido;
            }

            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                _logger.LogWarning(
                    "No se pudo notificar el ciclo de vida del equipo a {KeycloakId}: configuración SMTP incompleta.",
                    miembroKeycloakId);
                return ResultadoEnvio.Omitido;
            }

            await EnviarAsync(usuario, subject, plainText, ct);
            return ResultadoEnvio.Enviado;
        }
        catch (Exception ex)
        {
            // Timeout (deadline), SMTP caído o cualquier fallo de envío: el servidor no
            // respondió. Best-effort: se registra y se reporta, nunca se relanza.
            _logger.LogWarning(
                ex,
                "Fallo best-effort al notificar ciclo de vida de equipo al usuario con KeycloakId {KeycloakId}.",
                miembroKeycloakId);
            return ResultadoEnvio.ServidorNoRespondio;
        }
    }

    private async Task EnviarAsync(Usuario usuario, string subject, string plainText, CancellationToken ct)
    {
        using var smtp = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            // Acota la fase de conexión (SendMailAsync ignora esto, pero la cubre el token
            // con deadline; ambos juntos garantizan que el envío no cuelgue el request).
            Timeout = (int)NotificacionTimeout.TotalMilliseconds,
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
