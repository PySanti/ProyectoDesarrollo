namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

/// <summary>
/// Configuración del segundo consumidor RabbitMQ de Identity (7f, RNF-23): Identity se
/// autoconsume su propio evento <c>CredencialTemporalEmitida</c> (exchange <c>umbral.identity</c>,
/// no el de Operaciones) para disparar el correo SMTP de bienvenida de forma asíncrona. Sección
/// propia ("RabbitMqCredencialesConsumer") separada de <see cref="RabbitMqConsumerOptions"/>
/// porque exchange/cola/bindings difieren (mismo patrón: cada consumidor, su propia sección).
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
    };
}
