namespace Umbral.Partidas.Api.Workers;

// Bindings SOLO a los eventos de ciclo de vida de la partida: Partidas mantiene el estado como
// proyección de lo que Operaciones de Sesión reporta (fix 4). Calcado de Puntuaciones/Identity.
public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMqConsumer";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "partidas.operaciones-sesion.estado";
    public string[] Bindings { get; set; } =
    {
        "operaciones-sesion.partida-publicada-en-lobby.v1",
        "operaciones-sesion.partida-iniciada.v1",
        "operaciones-sesion.partida-cancelada.v1",
        "operaciones-sesion.partida-finalizada.v1",
    };
}
