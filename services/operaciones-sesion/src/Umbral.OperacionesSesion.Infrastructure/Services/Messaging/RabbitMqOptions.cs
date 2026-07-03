namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
}
