using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public sealed record EventEnvelope(Guid EventId, string EventType, int Version, DateTime OccurredAt, object Payload)
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static EventEnvelope Create(string eventType, object payload, DateTime occurredAtUtc)
        => new(Guid.NewGuid(), eventType, 1, occurredAtUtc, payload);
}
