namespace Umbral.Puntuaciones.Api.Workers;

// Segunda cola dedicada al historial (SP-4d). La conexión (host/credenciales/exchange/Enabled)
// se reusa de RabbitMqConsumerOptions; aquí solo viven la cola y su binding catch-all.
public sealed class RabbitMqHistorialOptions
{
    public const string SectionName = "RabbitMqHistorial";

    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.historial";
    public string Binding { get; set; } = "operaciones-sesion.#";
}
