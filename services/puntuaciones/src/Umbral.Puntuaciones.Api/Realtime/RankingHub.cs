using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Api.Realtime;

// Hub de ranking en vivo (SP-4c). Solo membresía de grupos: el repositorio se usa únicamente para
// validar que la partida exista en las proyecciones (paridad con ADR-0011); sin lógica de negocio.
// Lectura para cualquier rol autenticado, misma postura que los endpoints HTTP.
[Authorize]
public sealed class RankingHub : Hub
{
    private readonly IProyeccionesRepository _repo;

    public RankingHub(IProyeccionesRepository repo) => _repo = repo;

    public async Task SuscribirAPartida(Guid partidaId)
    {
        var partida = await _repo.GetPartidaAsync(partidaId, Context.ConnectionAborted);
        if (partida is null)
        {
            throw new HubException("Partida no proyectada.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RankingRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
    }

    public Task DesuscribirDePartida(Guid partidaId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RankingRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
}
