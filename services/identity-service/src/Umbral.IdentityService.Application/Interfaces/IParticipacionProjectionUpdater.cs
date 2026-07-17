using System.Text.Json;

namespace Umbral.IdentityService.Application.Interfaces;

/// <summary>
/// Aplica eventos de Operaciones de Sesión sobre la proyección local
/// <c>participaciones_activas_equipo</c> que alimenta el guard BR-E10 (no eliminar un
/// equipo con participación activa en una partida). Es best-effort e idempotente por
/// clave compuesta (equipoId, partidaId): eventos desconocidos o repetidos son no-op.
/// </summary>
public interface IParticipacionProjectionUpdater
{
    Task AplicarAsync(string eventType, JsonElement payload, CancellationToken cancellationToken);
}
