using System.Text.Json;

namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

/// <summary>
/// Parsea el envelope camelCase de eventos de Operaciones de Sesión
/// (<c>{ eventId, eventType, occurredAt, payload }</c>, ver <c>contracts/events</c>). Nunca lanza:
/// cualquier shape inesperado se reporta como malformado (<c>false</c>) para que el consumidor
/// lo trate best-effort (log + ack, ADR-0012).
/// </summary>
public static class OperacionesEnvelopeReader
{
    public static bool TryRead(ReadOnlySpan<byte> body, out string eventType, out JsonElement payload)
    {
        eventType = string.Empty;
        payload = default;
        try
        {
            using var doc = JsonDocument.Parse(body.ToArray());
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("eventType", out var type) || type.GetString() is not { Length: > 0 } parsedType ||
                !root.TryGetProperty("payload", out var parsedPayload) || parsedPayload.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            eventType = parsedType;
            payload = parsedPayload.Clone();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
