using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class HistorialNombreEnAltasTests
{
    // Fake mínimo del repo de historial que captura lo insertado.
    private sealed class FakeHistorialRepo : IHistorialNombreEquipoRepository
    {
        public List<HistorialNombreEquipo> Registros { get; } = new();

        public Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
        {
            Registros.AddRange(registros);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<HistorialNombreEquipo>>(Registros.Where(x => x.UsuarioId == usuarioId).ToList());

        public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(Registros.Count > 0);
    }

    private sealed class FakeEquipoRepositoryForCreacion : IEquipoRepository
    {
        public bool ExistsActiveTeamByUserIdValue { get; set; }
        public bool AddWasCalled { get; private set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsActiveTeamByUserIdValue);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            AddWasCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepositoryForInvitacion : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public bool InvitadoAlreadyHasTeam { get; set; }
        public bool UpdateWasCalled { get; private set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(InvitadoAlreadyHasTeam);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public InvitacionEquipo? InvitacionToReturn { get; set; }
        public bool UpdateWasCalled { get; private set; }

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult(InvitacionToReturn);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(new List<InvitacionEquipo>());

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(false);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeEquipoEventsPublisher : IIdentityEventsPublisher
    {
        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearEquipo_registra_historial_del_lider()
    {
        var actor = Guid.NewGuid();
        var equipoRepo = new FakeEquipoRepositoryForCreacion { ExistsActiveTeamByUserIdValue = false };
        var publisher = new FakeEquipoEventsPublisher();
        var historial = new FakeHistorialRepo();
        var handler = new CrearEquipoCommandHandler(equipoRepo, publisher, historial, TimeProvider.System);

        var response = await handler.Handle(new CrearEquipoCommand(actor, "Equipo A"), CancellationToken.None);

        var registro = Assert.Single(historial.Registros);
        Assert.Equal(actor, registro.UsuarioId);
        Assert.Equal("Equipo A", registro.NombreEquipo);
        Assert.Equal(response.EquipoId, registro.EquipoId);
    }

    [Fact]
    public async Task AceptarInvitacion_registra_historial_del_invitado()
    {
        var lider = Guid.NewGuid();
        var invitado = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, invitado, lider);

        var invRepo = new FakeInvitacionEquipoRepository { InvitacionToReturn = invitacion };
        var equipoRepo = new FakeEquipoRepositoryForInvitacion { TeamToReturn = equipo };
        var publisher = new FakeEquipoEventsPublisher();
        var historial = new FakeHistorialRepo();
        var handler = new AceptarInvitacionEquipoCommandHandler(invRepo, equipoRepo, publisher, historial, TimeProvider.System);

        await handler.Handle(new AceptarInvitacionEquipoCommand(invitado, invitacion.InvitacionEquipoId), CancellationToken.None);

        var registro = Assert.Single(historial.Registros);
        Assert.Equal(invitado, registro.UsuarioId);
        Assert.Equal("Equipo A", registro.NombreEquipo);
        Assert.Equal(equipo.EquipoId, registro.EquipoId);
    }
}
