using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public class RabbitMqIdentityEventsPublisherTests
{
    private sealed class CanalFake : IRabbitMqPublishChannel
    {
        public readonly List<(string RoutingKey, byte[] Body)> Publicados = new();
        public Exception? Lanzar { get; set; }
        public void Publish(string routingKey, byte[] body)
        {
            if (Lanzar is not null) throw Lanzar;
            Publicados.Add((routingKey, body));
        }
    }

    private static RabbitMqIdentityEventsPublisher CrearPublisher(CanalFake canal) =>
        new(canal, TimeProvider.System, NullLogger<RabbitMqIdentityEventsPublisher>.Instance);

    [Fact]
    public async Task Publica_envelope_camelCase_con_routing_key_correcta()
    {
        var canal = new CanalFake();
        var publisher = CrearPublisher(canal);

        await publisher.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        var (routingKey, body) = Assert.Single(canal.Publicados);
        Assert.Equal("identity.permisos-rol-actualizados.v1", routingKey);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("PermisosRolActualizados", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("payload", out var payload));
        Assert.Equal("Operador", payload.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Publica_CredencialTemporalEmitida_con_routing_key_y_password_en_el_payload()
    {
        var canal = new CanalFake();
        var publisher = CrearPublisher(canal);

        await publisher.PublishCredencialTemporalEmitidaAsync(
            new CredencialTemporalEmitidaIntegrationEvent("Ana", "ana@umbral.dev", "Participante", "Temp-Pass-1",
                new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        var (routingKey, body) = Assert.Single(canal.Publicados);
        Assert.Equal("identity.credencial-temporal-emitida.v1", routingKey);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("CredencialTemporalEmitida", doc.RootElement.GetProperty("eventType").GetString());
        var payload = doc.RootElement.GetProperty("payload");
        Assert.Equal("Ana", payload.GetProperty("nombre").GetString());
        Assert.Equal("ana@umbral.dev", payload.GetProperty("correo").GetString());
        Assert.Equal("Participante", payload.GetProperty("rol").GetString());
        Assert.Equal("Temp-Pass-1", payload.GetProperty("passwordTemporal").GetString());
    }

    [Fact]
    public async Task Fallo_del_canal_no_escapa_al_caller()
    {
        var canal = new CanalFake { Lanzar = new InvalidOperationException("broker caído") };
        var publisher = CrearPublisher(canal);

        var ex = await Record.ExceptionAsync(() => publisher.PublishRolUsuarioModificadoAsync(
            new RolUsuarioModificadoIntegrationEvent(Guid.NewGuid(), "Participante", "Operador", DateTime.UtcNow),
            CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cada_metodo_del_publisher_tiene_routing_key_en_el_mapa()
    {
        var canal = new CanalFake();
        var publisher = CrearPublisher(canal);
        var ahora = DateTime.UtcNow;

        await publisher.PublishEquipoCreadoAsync(new EquipoCreadoIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoCreadaAsync(new InvitacionEquipoCreadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoAceptadaAsync(new InvitacionEquipoAceptadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoRechazadaAsync(new InvitacionEquipoRechazadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishRolUsuarioModificadoAsync(new RolUsuarioModificadoIntegrationEvent(Guid.NewGuid(), "Participante", "Operador", ahora), CancellationToken.None);
        await publisher.PublishPermisosRolActualizadosAsync(new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, ahora), CancellationToken.None);

        Assert.Equal(6, canal.Publicados.Count);
        Assert.Equal(6, canal.Publicados.Select(p => p.RoutingKey).Distinct().Count());
        Assert.All(canal.Publicados, p => Assert.StartsWith("identity.", p.RoutingKey));
        Assert.All(canal.Publicados, p => Assert.EndsWith(".v1", p.RoutingKey));
    }
}
