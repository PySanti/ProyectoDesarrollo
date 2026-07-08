using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

using Umbral.IdentityService.Application.Handlers.Queries;
namespace Umbral.IdentityService.UnitTests.Teams.Invitations;

public sealed class GetInvitacionesRecibidasQueryHandlerTests
{
    [Fact]
    public async Task GetInvitacionesRecibidas_Returns_Empty_When_No_Pending_Invitations()
    {
        var invRepo = new FakeInvitacionEquipoRepository { PendientesToReturn = new List<InvitacionEquipo>() };
        var equipoRepo = new FakeEquipoRepository();
        var handler = new GetInvitacionesRecibidasQueryHandler(invRepo, equipoRepo);

        var result = await handler.Handle(new GetInvitacionesRecibidasQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInvitacionesRecibidas_Returns_Items_With_Correct_Fields()
    {
        var actorUserId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Test", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, actorUserId, lider);

        var invRepo = new FakeInvitacionEquipoRepository
        {
            PendientesToReturn = new List<InvitacionEquipo> { invitacion }
        };
        var equipoRepo = new FakeEquipoRepository { TeamById = equipo };
        var handler = new GetInvitacionesRecibidasQueryHandler(invRepo, equipoRepo);

        var result = await handler.Handle(new GetInvitacionesRecibidasQuery(actorUserId), CancellationToken.None);

        Assert.Single(result);
        var item = result[0];
        Assert.Equal(invitacion.InvitacionEquipoId, item.InvitacionId);
        Assert.Equal(equipo.EquipoId, item.EquipoId);
        Assert.Equal("Equipo Test", item.NombreEquipo);
        Assert.Equal(lider, item.InvitadoPorUserId);
    }

    [Fact]
    public async Task GetInvitacionesRecibidas_Returns_Empty_NombreEquipo_When_Team_Not_Found()
    {
        var actorUserId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo Ghost", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, actorUserId, lider);

        var invRepo = new FakeInvitacionEquipoRepository
        {
            PendientesToReturn = new List<InvitacionEquipo> { invitacion }
        };
        // No team registered in the repo — simulates team deleted after invitation was sent
        var equipoRepo = new FakeEquipoRepository { TeamById = null };
        var handler = new GetInvitacionesRecibidasQueryHandler(invRepo, equipoRepo);

        var result = await handler.Handle(new GetInvitacionesRecibidasQuery(actorUserId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].NombreEquipo);
    }

    // Fakes

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public List<InvitacionEquipo> PendientesToReturn { get; set; } = new();

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult<InvitacionEquipo?>(null);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(PendientesToReturn);

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(false);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamById { get; set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamById?.EquipoId == equipoId ? TeamById : null);

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Equipo>>(TeamById is null ? Array.Empty<Equipo>() : new[] { TeamById });

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
