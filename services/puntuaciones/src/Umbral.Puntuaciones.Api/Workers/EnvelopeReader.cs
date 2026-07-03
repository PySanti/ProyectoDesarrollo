using System.Text.Json;

namespace Umbral.Puntuaciones.Api.Workers;

public sealed record EnvelopeResumen(Guid EventId, string EventType, int Version, DateTime OccurredAt);

public static class EnvelopeReader
{
    public static bool TryRead(ReadOnlySpan<byte> body, out EnvelopeResumen? envelope)
    {
        envelope = null;
        try
        {
            using var doc = JsonDocument.Parse(body.ToArray());
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("eventId", out var id) || !id.TryGetGuid(out var eventId) ||
                !root.TryGetProperty("eventType", out var type) || type.GetString() is not { Length: > 0 } eventType ||
                !root.TryGetProperty("version", out var ver) || !ver.TryGetInt32(out var version) ||
                !root.TryGetProperty("occurredAt", out var at) || !at.TryGetDateTime(out var occurredAt))
            {
                return false;
            }
            envelope = new EnvelopeResumen(eventId, eventType, version, occurredAt);
            return true;
        }
        catch (Exception)
        {
            // Reader nunca lanza — cualquier shape inesperado es malformado.
            return false;
        }
    }
}
