using System.Text.Json;
using Umbral.IdentityService.Application.Services;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public sealed class ParticipacionProjectionUpdaterTests
{
    private sealed class FakeRepo : IParticipacionActivaEquipoRepository
    {
        public HashSet<(Guid, Guid)> Filas { get; } = new();
        public Task UpsertAsync(Guid e, Guid p, DateTime f, CancellationToken ct) { Filas.Add((e, p)); return Task.CompletedTask; }
        public Task RemoveByPartidaAsync(Guid p, CancellationToken ct) { Filas.RemoveWhere(x => x.Item2 == p); return Task.CompletedTask; }
        public Task RemoveAsync(Guid e, Guid p, CancellationToken ct) { Filas.Remove((e, p)); return Task.CompletedTask; }
        public Task<bool> ExistsByEquipoAsync(Guid e, CancellationToken ct) => Task.FromResult(Filas.Any(x => x.Item1 == e));
    }

    private static JsonElement Payload(object o) =>
        JsonSerializer.SerializeToElement(o, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task Creada_luego_Cancelada_deja_la_proyeccion_vacia()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        var equipo = Guid.NewGuid(); var partida = Guid.NewGuid();

        await updater.AplicarAsync("InscripcionEquipoCreada",
            Payload(new { equipoId = equipo, partidaId = partida, instante = DateTime.UtcNow }), CancellationToken.None);
        Assert.True(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));

        await updater.AplicarAsync("InscripcionEquipoCancelada",
            Payload(new { equipoId = equipo, partidaId = partida }), CancellationToken.None);
        Assert.False(await repo.ExistsByEquipoAsync(equipo, CancellationToken.None));
    }

    [Fact]
    public async Task PartidaFinalizada_limpia_por_partida()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        var partida = Guid.NewGuid();
        await updater.AplicarAsync("InscripcionEquipoCreada",
            Payload(new { equipoId = Guid.NewGuid(), partidaId = partida, instante = DateTime.UtcNow }), CancellationToken.None);

        await updater.AplicarAsync("PartidaFinalizada",
            Payload(new { partidaId = partida, sesionPartidaId = Guid.NewGuid(), fechaFin = DateTime.UtcNow }), CancellationToken.None);

        Assert.Empty(repo.Filas);
    }

    [Fact]
    public async Task Evento_desconocido_es_noop()
    {
        var repo = new FakeRepo();
        var updater = new ParticipacionProjectionUpdater(repo);
        await updater.AplicarAsync("JuegoActivado", Payload(new { partidaId = Guid.NewGuid() }), CancellationToken.None);
        Assert.Empty(repo.Filas);
    }
}
