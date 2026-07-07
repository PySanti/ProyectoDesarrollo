namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq";

    // Cola de humo de SP-3i: se elimina al arrancar (su binding # acumularía ubicaciones sin consumidor).
    public const string ColaDeHumoLegacy = "puntuaciones.operaciones-sesion.all";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.proyecciones";
    public string[] Bindings { get; set; } =
    {
        "operaciones-sesion.partida-publicada-en-lobby.v1",
        "operaciones-sesion.partida-iniciada.v1",
        "operaciones-sesion.juego-activado.v1",
        "operaciones-sesion.partida-cancelada.v1",
        "operaciones-sesion.partida-finalizada.v1",
        "operaciones-sesion.puntaje-trivia-incrementado.v1",
        "operaciones-sesion.etapa-bdt-ganada.v1",
    };
}
