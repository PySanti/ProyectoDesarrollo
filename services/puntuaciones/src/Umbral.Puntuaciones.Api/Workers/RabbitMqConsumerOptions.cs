namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.all";
    public string Binding { get; set; } = "operaciones-sesion.#";
}
