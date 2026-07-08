namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

/// <summary>
/// Configuración del primer consumidor RabbitMQ de Identity (Task D3): escucha eventos de
/// inscripción/fin de partida de Operaciones de Sesión para mantener la proyección
/// <c>participaciones_activas_equipo</c> (BR-E10). Sección propia ("RabbitMqConsumer") separada
/// de <see cref="RabbitMqOptions"/> (publisher de Identity) porque exchange/cola difieren.
/// </summary>
public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMqConsumer";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "identity.operaciones-sesion.participaciones";
    public string[] Bindings { get; set; } =
    {
        "operaciones-sesion.inscripcion-equipo-creada.v1",
        "operaciones-sesion.inscripcion-equipo-cancelada.v1",
        "operaciones-sesion.partida-finalizada.v1",
        "operaciones-sesion.partida-cancelada.v1",
    };
}
