namespace Umbral.IdentityService.Infrastructure.Services.Notifications;

/// <summary>
/// Construye los correos de ciclo de vida de equipo (eliminación y cambio de liderazgo).
/// Métodos puros (sin I/O) para que sean testeables sin SMTP.
/// </summary>
public static class TeamLifecycleEmailTemplate
{
    public static (string Subject, string PlainText) BuildEquipoEliminado(string nombreEquipo)
    {
        const string subject = "Tu equipo en UMBRAL ha sido eliminado";
        var body = $"""
        UMBRAL

        El equipo "{nombreEquipo}" ha sido eliminado por un administrador.

        Ya no formas parte de este equipo. Si crees que se trata de un error,
        contacta a un administrador de la plataforma.

        UMBRAL · Trivia y Búsqueda del Tesoro en tiempo real
        """;

        return (subject, body);
    }

    public static (string Subject, string PlainText) BuildLiderazgo(bool esNuevoLider)
    {
        const string subject = "Cambio de liderazgo en tu equipo de UMBRAL";
        var body = esNuevoLider
            ? """
              UMBRAL

              Ahora eres el líder de tu equipo en UMBRAL.

              A partir de este momento puedes gestionar las invitaciones y la
              información del equipo.

              UMBRAL · Trivia y Búsqueda del Tesoro en tiempo real
              """
            : """
              UMBRAL

              Se ha designado un nuevo líder para tu equipo en UMBRAL.

              Ya no ejerces el liderazgo, pero sigues siendo integrante del equipo.

              UMBRAL · Trivia y Búsqueda del Tesoro en tiempo real
              """;

        return (subject, body);
    }
}
