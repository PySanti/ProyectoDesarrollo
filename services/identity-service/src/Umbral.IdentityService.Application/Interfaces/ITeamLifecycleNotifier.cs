namespace Umbral.IdentityService.Application.Interfaces;

/// <summary>
/// Notifica por correo eventos del ciclo de vida de un equipo (eliminación, cambio de
/// liderazgo). Es <b>best-effort</b> (ADR-0012): la implementación de infraestructura debe
/// tragar y loguear cualquier fallo (SMTP caído, destinatario no resoluble) y nunca lanzar,
/// a diferencia de <see cref="IUserWelcomeEmailSender"/>.
/// </summary>
public interface ITeamLifecycleNotifier
{
    /// <summary>
    /// Notifica la eliminación del equipo. Devuelve un <see cref="TeamNotificationOutcome"/>
    /// para que el llamador informe al operador cuántos integrantes fueron notificados y si el
    /// servidor de correo respondió. Best-effort: no lanza.
    /// </summary>
    Task<TeamNotificationOutcome> NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct);

    Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct);
}
