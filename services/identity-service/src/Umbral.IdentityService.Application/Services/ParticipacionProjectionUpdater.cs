using System.Text.Json;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Services;

/// <summary>
/// Mapea eventos de Operaciones de Sesión (inscripción de equipo / fin de partida) a
/// operaciones sobre <see cref="IParticipacionActivaEquipoRepository"/>. Ver
/// <see cref="IParticipacionProjectionUpdater"/> para el contrato best-effort/idempotente.
/// </summary>
public sealed class ParticipacionProjectionUpdater : IParticipacionProjectionUpdater
{
    private readonly IParticipacionActivaEquipoRepository _repo;

    public ParticipacionProjectionUpdater(IParticipacionActivaEquipoRepository repo) => _repo = repo;

    public async Task AplicarAsync(string eventType, JsonElement payload, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case "InscripcionEquipoCreada":
                await _repo.UpsertAsync(
                    LeerGuid(payload, "equipoId"),
                    LeerGuid(payload, "partidaId"),
                    DateTime.UtcNow,
                    cancellationToken);
                break;
            case "InscripcionEquipoCancelada":
                await _repo.RemoveAsync(
                    LeerGuid(payload, "equipoId"),
                    LeerGuid(payload, "partidaId"),
                    cancellationToken);
                break;
            case "PartidaFinalizada":
            case "PartidaCancelada":
                await _repo.RemoveByPartidaAsync(LeerGuid(payload, "partidaId"), cancellationToken);
                break;
            default:
                // Evento sin proyección conocida: no-op (best-effort, ADR-0012).
                break;
        }
    }

    private static Guid LeerGuid(JsonElement payload, string prop) =>
        payload.TryGetProperty(prop, out var valor) ? valor.GetGuid() : Guid.Empty;
}
