using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Persistence;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.IntegrationTests.Teams;

public sealed class InvitacionEquipoPersistenceTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"invitaciones-integration-{Guid.NewGuid():N}")
            .Options;
        return new IdentityDbContext(options);
    }

    [Fact]
    public void IdentityDbContext_Maps_Invitaciones_Equipo_Fk_To_Equipos_With_Cascade_Delete()
    {
        using var dbContext = CreateInMemoryDbContext();

        var invitacionEntity = dbContext.Model.FindEntityType(typeof(InvitacionEquipo));
        Assert.NotNull(invitacionEntity);

        var fk = Assert.Single(invitacionEntity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Equipo));
        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
        Assert.Equal("equipoid", fk.Properties.Single().GetColumnName(StoreObjectIdentifier.Table("invitaciones_equipo", null)));
        Assert.Equal("equipos", fk.PrincipalEntityType.GetTableName());
    }

    [Fact]
    public async Task Invite_Accept_Adds_Invitee_As_Member_And_Sets_Estado_Aceptada()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Test", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        // Create and persist invitation
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        await invRepo.AddAsync(invitacion, CancellationToken.None);

        var historialRepo = new HistorialNombreEquipoRepository(dbContext);
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, new NoOpIdentityEventsPublisher(), historialRepo, TimeProvider.System);
        var response = await handler.Handle(
            new AceptarInvitacionEquipoCommand(invitadoUserId, invitacion.InvitacionEquipoId),
            CancellationToken.None);

        var persisted = await dbContext.InvitacionesEquipo
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvitacionEquipoId == invitacion.InvitacionEquipoId);
        Assert.NotNull(persisted);
        Assert.Equal(EstadoInvitacion.Aceptada, persisted.Estado);
        Assert.Equal(EstadoInvitacion.Aceptada.ToString(), response.EstadoInvitacion);

        var persistedEquipo = await dbContext.Equipos
            .AsNoTracking()
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);
        Assert.NotNull(persistedEquipo);
        Assert.Contains(persistedEquipo.Participantes, p => p.UsuarioId == invitadoUserId && !p.EsLider);
    }

    [Fact]
    public async Task Invite_Reject_Sets_Rechazada()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Rechazo", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        await invRepo.AddAsync(invitacion, CancellationToken.None);

        // Simulate handler: load invitation fresh, then reject
        var loaded = await invRepo.GetByIdAsync(invitacion.InvitacionEquipoId, CancellationToken.None);
        Assert.NotNull(loaded);
        loaded.Rechazar();
        await invRepo.UpdateAsync(loaded, CancellationToken.None);

        var persistedInvitacion = await dbContext.InvitacionesEquipo
            .FirstOrDefaultAsync(i => i.InvitacionEquipoId == invitacion.InvitacionEquipoId);
        Assert.NotNull(persistedInvitacion);
        Assert.Equal(EstadoInvitacion.Rechazada, persistedInvitacion.Estado);

        // Invitee should NOT be a member
        var persistedEquipo = await dbContext.Equipos
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipo.EquipoId);
        Assert.NotNull(persistedEquipo);
        Assert.Single(persistedEquipo.Participantes);
        Assert.DoesNotContain(persistedEquipo.Participantes, p => p.UsuarioId == invitadoUserId);
    }

    [Fact]
    public async Task SoleLeader_Leaves_Deletes_Pending_Invitations()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId1 = Guid.NewGuid();
        var invitadoUserId2 = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Eliminado", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        // Add two pending invitations
        var inv1 = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId1, liderUserId);
        var inv2 = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId2, liderUserId);
        await invRepo.AddAsync(inv1, CancellationToken.None);
        await invRepo.AddAsync(inv2, CancellationToken.None);

        var handler = new SalirDeEquipoCommandHandler(equipoRepo, invRepo);
        var response = await handler.Handle(new SalirDeEquipoCommand(liderUserId), CancellationToken.None);

        Assert.Equal(ResultadoSalidaEquipo.EquipoEliminado.ToString(), response.Resultado);

        // Assert no pending invitations remain for this team
        var remaining = await dbContext.InvitacionesEquipo
            .Where(i => i.EquipoId == equipo.EquipoId && i.Estado == EstadoInvitacion.Pendiente)
            .ToListAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetPendientesByInvitadoAsync_Returns_Only_Pending_For_User()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Query", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        var pendiente = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        await invRepo.AddAsync(pendiente, CancellationToken.None);

        var rechazada = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        rechazada.Rechazar();
        await invRepo.AddAsync(rechazada, CancellationToken.None);
        await invRepo.UpdateAsync(rechazada, CancellationToken.None);

        var result = await invRepo.GetPendientesByInvitadoAsync(invitadoUserId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(pendiente.InvitacionEquipoId, result[0].InvitacionEquipoId);
        Assert.Equal(EstadoInvitacion.Pendiente, result[0].Estado);
    }

    [Fact]
    public async Task ExistsPendienteAsync_Returns_True_When_Pending_Exists()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Exists", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        var inv = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        await invRepo.AddAsync(inv, CancellationToken.None);

        var exists = await invRepo.ExistsPendienteAsync(equipo.EquipoId, invitadoUserId, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsPendienteAsync_Returns_False_After_Acceptance()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var equipoRepo = new EquipoRepository(dbContext);
        var invRepo = new InvitacionEquipoRepository(dbContext);

        var liderUserId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Accepted", liderUserId);
        await equipoRepo.AddAsync(equipo, CancellationToken.None);

        var inv = InvitacionEquipo.Crear(equipo.EquipoId, invitadoUserId, liderUserId);
        await invRepo.AddAsync(inv, CancellationToken.None);
        var loaded = await invRepo.GetByIdAsync(inv.InvitacionEquipoId, CancellationToken.None);
        Assert.NotNull(loaded);
        loaded.Aceptar();
        await invRepo.UpdateAsync(loaded, CancellationToken.None);

        var exists = await invRepo.ExistsPendienteAsync(equipo.EquipoId, invitadoUserId, CancellationToken.None);

        Assert.False(exists);
    }

}
