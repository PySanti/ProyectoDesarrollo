using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class EquipoPersistenceTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"equipos-integration-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task AddAsync_Should_Persist_Equipo_With_Leader()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var creadorUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo Test", creadorUserId);

        await repository.AddAsync(equipo, CancellationToken.None);

        var persisted = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);

        Assert.NotNull(persisted);
        Assert.Equal("Equipo Test", persisted.NombreEquipo);
        Assert.Equal(EstadoEquipo.Activo, persisted.Estado);
        Assert.Single(persisted.Participantes);
        Assert.True(persisted.Participantes.Single().EsLider);
        Assert.Equal(creadorUserId, persisted.Participantes.Single().UsuarioId);
    }

    [Fact]
    public async Task ExistsActiveTeamByUserIdAsync_Should_Return_True_When_User_Is_Member()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var creadorUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo B", creadorUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        var exists = await repository.ExistsActiveTeamByUserIdAsync(creadorUserId, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsActiveTeamByUserIdAsync_Should_Return_False_For_Non_Member()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var creadorUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo C", creadorUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        var exists = await repository.ExistsActiveTeamByUserIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateAsync_NonLeader_Leaves_Team_Removes_Member_And_Keeps_Leader()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var miembroUserId = Guid.NewGuid();

        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo D", liderUserId);
        equipo.AgregarParticipante(miembroUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        // Member leaves
        var resultado = equipo.Salir(miembroUserId);
        await repository.UpdateAsync(equipo, CancellationToken.None);

        Assert.Equal(Domain.Enums.ResultadoSalidaEquipo.SalioDelEquipo, resultado);

        var persisted = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);

        Assert.NotNull(persisted);
        Assert.Equal(EstadoEquipo.Activo, persisted.Estado);
        Assert.Single(persisted.Participantes);
        Assert.Equal(1, persisted.Participantes.Count(p => p.EsLider));
        Assert.Equal(liderUserId, persisted.Participantes.Single(p => p.EsLider).UsuarioId);
        Assert.DoesNotContain(persisted.Participantes, p => p.UsuarioId == miembroUserId);
    }

    [Fact]
    public async Task UpdateAsync_Transfer_Leadership_Persists_Exactly_One_Leader()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var nuevoLiderUserId = Guid.NewGuid();

        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo E", liderUserId);
        equipo.AgregarParticipante(nuevoLiderUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        equipo.TransferirLiderazgo(liderUserId, nuevoLiderUserId);
        await repository.UpdateAsync(equipo, CancellationToken.None);

        var persisted = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);

        Assert.NotNull(persisted);
        Assert.Equal(EstadoEquipo.Activo, persisted.Estado);
        Assert.Equal(2, persisted.Participantes.Count);
        Assert.Equal(1, persisted.Participantes.Count(p => p.EsLider));
        Assert.True(persisted.Participantes.Single(p => p.UsuarioId == nuevoLiderUserId).EsLider);
        Assert.False(persisted.Participantes.Single(p => p.UsuarioId == liderUserId).EsLider);
    }

    [Fact]
    public async Task UpdateAsync_Leader_Leaves_Alone_Sets_Team_Deleted()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo F", liderUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        // Leader is the only member — leaving deletes the team
        var resultado = equipo.Salir(liderUserId);
        await repository.UpdateAsync(equipo, CancellationToken.None);

        Assert.Equal(Domain.Enums.ResultadoSalidaEquipo.EquipoEliminado, resultado);

        var persisted = await dbContext.Equipos
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);

        Assert.NotNull(persisted);
        Assert.Equal(EstadoEquipo.Eliminado, persisted.Estado);
    }

    [Fact]
    public async Task GetActiveByMemberUserIdAsync_Should_Return_Team_For_Active_Member()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var creadorUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo G", creadorUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        var found = await repository.GetActiveByMemberUserIdAsync(creadorUserId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(equipo.EquipoId, found.EquipoId);
        Assert.NotEmpty(found.Participantes);
    }

    [Fact]
    public async Task GetActiveByMemberUserIdAsync_Should_Return_Null_For_Deleted_Team()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var equipo = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo H", liderUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        // Leader leaves alone, deletes the team
        equipo.Salir(liderUserId);
        await repository.UpdateAsync(equipo, CancellationToken.None);

        var found = await repository.GetActiveByMemberUserIdAsync(liderUserId, CancellationToken.None);

        Assert.Null(found);
    }

    /// <summary>
    /// Regression test for the cross-equipo contamination bug in UpdateAsync.
    /// When two Equipo aggregates are loaded in the same DbContext scope and
    /// UpdateAsync is called on one of them, the ChangeTracker scan must NOT
    /// mark members of the OTHER equipo as Deleted.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Removing_Member_From_TeamA_Does_Not_Affect_TeamB_Members()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var repository = new EquipoRepository(dbContext);

        // --- Arrange: create two teams, each with a leader + one extra member ---
        var liderAId = Guid.NewGuid();
        var miembroAId = Guid.NewGuid();
        var liderBId = Guid.NewGuid();
        var miembroBId = Guid.NewGuid();

        var equipoA = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo A", liderAId);
        equipoA.AgregarParticipante(miembroAId);
        await repository.AddAsync(equipoA, CancellationToken.None);

        var equipoB = Umbral.IdentityService.Domain.Entities.Equipo.CrearPorParticipante("Equipo B", liderBId);
        equipoB.AgregarParticipante(miembroBId);
        await repository.AddAsync(equipoB, CancellationToken.None);

        // --- Act: load BOTH aggregates in the same DbContext scope ---
        var loadedA = await repository.GetActiveByMemberUserIdAsync(liderAId, CancellationToken.None);
        var loadedB = await repository.GetActiveByMemberUserIdAsync(liderBId, CancellationToken.None);

        Assert.NotNull(loadedA);
        Assert.NotNull(loadedB);

        // Remove a non-leader member from team A only
        var resultado = loadedA.Salir(miembroAId);
        Assert.Equal(Domain.Enums.ResultadoSalidaEquipo.SalioDelEquipo, resultado);

        await repository.UpdateAsync(loadedA, CancellationToken.None);

        // --- Assert: team A lost exactly miembroA ---
        var persistedA = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == loadedA.EquipoId);

        Assert.NotNull(persistedA);
        Assert.Single(persistedA.Participantes);
        Assert.DoesNotContain(persistedA.Participantes, p => p.UsuarioId == miembroAId);
        Assert.Contains(persistedA.Participantes, p => p.UsuarioId == liderAId);

        // --- Assert: team B still has ALL its members (the critical cross-team check) ---
        var persistedB = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == loadedB.EquipoId);

        Assert.NotNull(persistedB);
        Assert.Equal(2, persistedB.Participantes.Count);
        Assert.Contains(persistedB.Participantes, p => p.UsuarioId == liderBId);
        Assert.Contains(persistedB.Participantes, p => p.UsuarioId == miembroBId);
    }
}
