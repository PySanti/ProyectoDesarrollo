namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

/// <summary>
/// Configuración del segundo consumidor RabbitMQ de Identity (7f, RNF-23): la cola de <b>correos
/// de Identity</b>. Identity se autoconsume sus propios eventos (exchange <c>umbral.identity</c>,
/// no el de Operaciones) para enviar correo SMTP de forma asíncrona, sin bloquear la operación de
/// dominio que lo origina: <c>CredencialTemporalEmitida</c> (bienvenida), <c>EquipoEliminado</c>
/// (aviso a los integrantes) y <c>LiderazgoEquipoModificado</c> (aviso a ambos líderes). Sección
/// propia ("RabbitMqCredencialesConsumer") separada de
/// <see cref="RabbitMqConsumerOptions"/> porque exchange/cola/bindings difieren (mismo patrón:
/// cada consumidor, su propia sección). El nombre de la sección y de la cola se conservan por
/// compatibilidad con la configuración y la cola durable ya desplegadas.
/// </summary>
public sealed class RabbitMqCredencialesConsumerOptions
{
    public const string SectionName = "RabbitMqCredencialesConsumer";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.identity";
    public string Queue { get; set; } = "identity.correo-credenciales";
    public string[] Bindings { get; set; } =
    {
        "identity.credencial-temporal-emitida.v1",
        "identity.equipo-eliminado.v1",
        "identity.liderazgo-equipo-modificado.v1",
    };
}
