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

    private sealed class FakeTeamLifecycleNotifier : ITeamLifecycleNotifier
    {
        private readonly bool _throwOnNotify;
        public string? NotifiedNombreEquipo { get; private set; }
        public IReadOnlyList<Guid>? NotifiedMiembros { get; private set; }
        public Guid? NotifiedLiderAnterior { get; private set; }
        public Guid? NotifiedNuevoLider { get; private set; }

        public FakeTeamLifecycleNotifier(bool throwOnNotify = false) => _throwOnNotify = throwOnNotify;

        public Task<TeamNotificationOutcome> NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct)
        {
            if (_throwOnNotify)
            {
                throw new EmailDeliveryException("forced SMTP failure for test");
            }

            NotifiedNombreEquipo = nombreEquipo;
            NotifiedMiembros = miembros;
            return Task.FromResult(new TeamNotificationOutcome(miembros.Count, miembros.Count, 0));
        }

        public Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct)
        {
            if (_throwOnNotify)
            {
                throw new EmailDeliveryException("forced SMTP failure for test");
            }

            NotifiedLiderAnterior = liderAnteriorUserId;
            NotifiedNuevoLider = nuevoLiderUserId;
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

    // ── EquipoEliminado ───────────────────────────────────────────────────────

    private const string RkEquipoEliminado = "identity.equipo-eliminado.v1";

    [Fact]
    public async Task NotificarEquipoEliminado_notifica_a_todos_los_miembros_del_payload()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var payload = Payload(new
        {
            equipoId = Guid.NewGuid(),
            nombreEquipo = "Equipo A",
            origen = "Lider",
            miembros = new[] { lider, miembro },
            occurredOnUtc = DateTime.UtcNow
        });
        var notifier = new FakeTeamLifecycleNotifier();

        await CredencialesTemporalesConsumer.NotificarEquipoEliminadoBestEffortAsync(
            notifier, payload, NullLogger.Instance, RkEquipoEliminado, CancellationToken.None);

        Assert.Equal("Equipo A", notifier.NotifiedNombreEquipo);
        Assert.NotNull(notifier.NotifiedMiembros);
        Assert.Equal(2, notifier.NotifiedMiembros!.Count);
        Assert.Contains(lider, notifier.NotifiedMiembros);
        Assert.Contains(miembro, notifier.NotifiedMiembros);
    }

    [Fact]
    public async Task NotificarEquipoEliminado_sin_miembros_no_llama_al_notificador()
    {
        var payload = Payload(new { nombreEquipo = "Equipo A", miembros = Array.Empty<Guid>() });
        var notifier = new FakeTeamLifecycleNotifier();

        await CredencialesTemporalesConsumer.NotificarEquipoEliminadoBestEffortAsync(
            notifier, payload, NullLogger.Instance, RkEquipoEliminado, CancellationToken.None);

        Assert.Null(notifier.NotifiedNombreEquipo);
    }

    [Fact]
    public async Task NotificarEquipoEliminado_con_payload_roto_no_lanza()
    {
        // miembros ausente, y con un valor que no es guid: best-effort, nunca relanza (ack siempre).
        var payloadSinMiembros = Payload(new { nombreEquipo = "Equipo A" });
        var payloadMiembrosInvalidos = Payload(new { nombreEquipo = "Equipo A", miembros = new[] { "no-es-guid" } });
        var notifier = new FakeTeamLifecycleNotifier();

        var sinMiembros = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarEquipoEliminadoBestEffortAsync(
            notifier, payloadSinMiembros, NullLogger.Instance, RkEquipoEliminado, CancellationToken.None));
        var invalidos = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarEquipoEliminadoBestEffortAsync(
            notifier, payloadMiembrosInvalidos, NullLogger.Instance, RkEquipoEliminado, CancellationToken.None));

        Assert.Null(sinMiembros);
        Assert.Null(invalidos);
        Assert.Null(notifier.NotifiedNombreEquipo);
    }

    [Fact]
    public async Task NotificarEquipoEliminado_si_el_SMTP_lanza_no_relanza_best_effort()
    {
        var payload = Payload(new { nombreEquipo = "Equipo A", miembros = new[] { Guid.NewGuid() } });
        var notifier = new FakeTeamLifecycleNotifier(throwOnNotify: true);

        var exception = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarEquipoEliminadoBestEffortAsync(
            notifier, payload, NullLogger.Instance, RkEquipoEliminado, CancellationToken.None));

        Assert.Null(exception);
    }

    // ── LiderazgoEquipoModificado ─────────────────────────────────────────────

    private const string RkLiderazgo = "identity.liderazgo-equipo-modificado.v1";

    [Fact]
    public async Task NotificarLiderazgo_notifica_al_lider_anterior_y_al_nuevo()
    {
        var liderAnterior = Guid.NewGuid();
        var nuevoLider = Guid.NewGuid();
        var payload = Payload(new
        {
            equipoId = Guid.NewGuid(),
            liderAnteriorUserId = liderAnterior,
            nuevoLiderUserId = nuevoLider,
            origen = "Admin",
            occurredOnUtc = DateTime.UtcNow
        });
        var notifier = new FakeTeamLifecycleNotifier();

        await CredencialesTemporalesConsumer.NotificarLiderazgoModificadoBestEffortAsync(
            notifier, payload, NullLogger.Instance, RkLiderazgo, CancellationToken.None);

        Assert.Equal(liderAnterior, notifier.NotifiedLiderAnterior);
        Assert.Equal(nuevoLider, notifier.NotifiedNuevoLider);
    }

    [Fact]
    public async Task NotificarLiderazgo_con_payload_incompleto_no_notifica_ni_lanza()
    {
        // Falta nuevoLiderUserId, y un id que no es guid: best-effort, nunca relanza (ack siempre).
        var payloadIncompleto = Payload(new { liderAnteriorUserId = Guid.NewGuid() });
        var payloadInvalido = Payload(new { liderAnteriorUserId = "no-es-guid", nuevoLiderUserId = Guid.NewGuid() });
        var notifier = new FakeTeamLifecycleNotifier();

        var incompleto = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarLiderazgoModificadoBestEffortAsync(
            notifier, payloadIncompleto, NullLogger.Instance, RkLiderazgo, CancellationToken.None));
        var invalido = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarLiderazgoModificadoBestEffortAsync(
            notifier, payloadInvalido, NullLogger.Instance, RkLiderazgo, CancellationToken.None));

        Assert.Null(incompleto);
        Assert.Null(invalido);
        Assert.Null(notifier.NotifiedLiderAnterior);
        Assert.Null(notifier.NotifiedNuevoLider);
    }

    [Fact]
    public async Task NotificarLiderazgo_si_el_SMTP_lanza_no_relanza_best_effort()
    {
        var payload = Payload(new { liderAnteriorUserId = Guid.NewGuid(), nuevoLiderUserId = Guid.NewGuid() });
        var notifier = new FakeTeamLifecycleNotifier(throwOnNotify: true);

        var exception = await Record.ExceptionAsync(() => CredencialesTemporalesConsumer.NotificarLiderazgoModificadoBestEffortAsync(
            notifier, payload, NullLogger.Instance, RkLiderazgo, CancellationToken.None));

        Assert.Null(exception);
    }
}
