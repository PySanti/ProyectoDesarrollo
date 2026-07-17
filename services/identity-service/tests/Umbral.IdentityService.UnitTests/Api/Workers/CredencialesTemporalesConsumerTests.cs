using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Api.Workers;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Api.Workers;

// Cubre la parte pura/testable del consumidor (7f, RNF-23): mapeo del payload y el envío
// best-effort, sin necesidad de un broker RabbitMQ real (mismo enfoque que
// ParticipacionProjectionUpdaterTests para el primer consumidor de Identity).
public sealed class CredencialesTemporalesConsumerTests
{
    private sealed class FakeWelcomeEmailSender : IUserWelcomeEmailSender
    {
        private readonly bool _throwOnSend;
        public UserWelcomeEmailMessage? LastMessage { get; private set; }

        public FakeWelcomeEmailSender(bool throwOnSend = false) => _throwOnSend = throwOnSend;

        public Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken)
        {
            if (_throwOnSend)
            {
                throw new EmailDeliveryException("forced SMTP failure for test");
            }

            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private static JsonElement Payload(object o) =>
        JsonSerializer.SerializeToElement(o, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public void MapPayload_mapea_el_envelope_camelCase_a_UserWelcomeEmailMessage()
    {
        var payload = Payload(new
        {
            nombre = "Ana Pérez",
            correo = "ana@example.com",
            rol = "Operador",
            passwordTemporal = "Tmp#12345",
            occurredOnUtc = DateTime.UtcNow
        });

        var mensaje = CredencialesTemporalesConsumer.MapPayload(payload);

        Assert.Equal("Ana Pérez", mensaje.Name);
        Assert.Equal("ana@example.com", mensaje.Email);
        Assert.Equal("Operador", mensaje.Role);
        Assert.Equal("Tmp#12345", mensaje.TemporaryPassword);
    }

    [Fact]
    public void MapPayload_con_campos_ausentes_no_lanza_y_usa_vacio()
    {
        var payload = Payload(new { });

        var mensaje = CredencialesTemporalesConsumer.MapPayload(payload);

        Assert.Equal(string.Empty, mensaje.Name);
        Assert.Equal(string.Empty, mensaje.Email);
        Assert.Equal(string.Empty, mensaje.Role);
        Assert.Equal(string.Empty, mensaje.TemporaryPassword);
    }

    [Fact]
    public async Task EnviarBestEffortAsync_llama_al_sender_con_el_mensaje_mapeado()
    {
        var payload = Payload(new { nombre = "Ana", correo = "ana@example.com", rol = "Participante", passwordTemporal = "abc123" });
        var sender = new FakeWelcomeEmailSender();

        await CredencialesTemporalesConsumer.EnviarBestEffortAsync(
            sender, payload, NullLogger.Instance, "identity.credencial-temporal-emitida.v1", CancellationToken.None);

        Assert.NotNull(sender.LastMessage);
        Assert.Equal("ana@example.com", sender.LastMessage!.Email);
    }

    [Fact]
    public async Task EnviarBestEffortAsync_si_el_SMTP_lanza_no_relanza_best_effort()
    {
        var payload = Payload(new { nombre = "Ana", correo = "ana@example.com", rol = "Participante", passwordTemporal = "abc123" });
        var sender = new FakeWelcomeEmailSender(throwOnSend: true);

        var exception = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.EnviarBestEffortAsync(
            sender, payload, NullLogger.Instance, "identity.credencial-temporal-emitida.v1", CancellationToken.None));

        Assert.Null(exception);
    }
}
