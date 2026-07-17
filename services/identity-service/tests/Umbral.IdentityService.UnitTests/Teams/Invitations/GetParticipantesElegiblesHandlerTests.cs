using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Handlers.Queries;
namespace Umbral.IdentityService.UnitTests.Teams.Invitations;

public sealed class GetParticipantesElegiblesHandlerTests
{
    [Fact]
    public async Task GetElegibles_Throws_NoEsLider_When_Actor_Has_No_Active_Team()
    {
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = null };
        var usuarioRepo = new FakeUsuarioRepository();
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        await Assert.ThrowsAsync<NoEsLiderException>(() =>
            handler.Handle(new GetParticipantesElegiblesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GetElegibles_Throws_NoEsLider_When_Actor_Is_Not_Leader()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(miembro);

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var usuarioRepo = new FakeUsuarioRepository();
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        await Assert.ThrowsAsync<NoEsLiderException>(() =>
            handler.Handle(new GetParticipantesElegiblesQuery(miembro), CancellationToken.None));
    }

    [Fact]
    public async Task GetElegibles_Returns_Empty_When_Team_Is_Full()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());

        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var usuarioRepo = new FakeUsuarioRepository();
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(lider), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Users_Already_In_A_Team()
    {
        // El equipo indexa a sus miembros por el sub de Keycloak (ParticipanteEquipo.UsuarioId),
        // que es el KeycloakId del Usuario, no su UsuarioId local.
        var liderSub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", liderSub);

        var eligibleSub = Guid.NewGuid();
        var inTeamSub = Guid.NewGuid();

        var eligibleUser = Usuario.Crear(eligibleSub.ToString(), "Elegible", "elegible@test.com", RolUsuario.Participante);
        var inTeamUser = Usuario.Crear(inTeamSub.ToString(), "EnEquipo", "enequipo@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { eligibleUser, inTeamUser }
        };

        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid> { inTeamSub }
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(liderSub), CancellationToken.None);

        Assert.Single(result);
        // Devuelve el sub de Keycloak, no el UsuarioId local: el movil lo reenvia como invitadoUserId.
        Assert.Equal(eligibleSub, result[0].UserId);
        Assert.Equal("Elegible", result[0].Nombre);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Current_Team_Members_Including_Leader()
    {
        // Regresion del bug reportado: el lider se veia a si mismo en la lista de invitables.
        // El lider ES un Usuario local cuyo KeycloakId == el sub con el que se creo el equipo;
        // debe quedar excluido.
        var liderSub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", liderSub);

        var liderUser = Usuario.Crear(liderSub.ToString(), "Lider", "lider@test.com", RolUsuario.Participante);
        var otherUser = Usuario.Crear(Guid.NewGuid().ToString(), "Otro", "otro@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { liderUser, otherUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(liderSub), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Otro", result[0].Nombre);
        Assert.DoesNotContain(result, r => r.Nombre == "Lider");
    }

    [Fact]
    public async Task GetElegibles_Excludes_Non_Participante_Role_Users()
    {
        var liderSub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", liderSub);

        var participanteSub = Guid.NewGuid();
        var adminUser = Usuario.Crear(Guid.NewGuid().ToString(), "Admin", "admin@test.com", RolUsuario.Administrador);
        var participanteUser = Usuario.Crear(participanteSub.ToString(), "Participante", "p@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { adminUser, participanteUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, new FakeInvitacionEquipoRepository());

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(liderSub), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(participanteSub, result[0].UserId);
    }

    [Fact]
    public async Task GetElegibles_Marks_YaInvitado_For_Candidates_With_Pending_Invitation()
    {
        var liderSub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", liderSub);

        var invitadoSub = Guid.NewGuid();
        var noInvitadoSub = Guid.NewGuid();
        var invitadoUser = Usuario.Crear(invitadoSub.ToString(), "Invitado", "invitado@test.com", RolUsuario.Participante);
        var noInvitadoUser = Usuario.Crear(noInvitadoSub.ToString(), "NoInvitado", "noinvitado@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { invitadoUser, noInvitadoUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };
        var invitacionRepo = new FakeInvitacionEquipoRepository
        {
            PendientesByEquipo = new HashSet<Guid> { invitadoSub }
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo, invitacionRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(liderSub), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(r => r.UserId == invitadoSub).YaInvitado);
        Assert.False(result.Single(r => r.UserId == noInvitadoSub).YaInvitado);
    }

    // Fakes

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public HashSet<Guid> PendientesByEquipo { get; set; } = new();

        public Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>(PendientesByEquipo);

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct) => Task.CompletedTask;
        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct) => Task.FromResult<InvitacionEquipo?>(null);
        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(Array.Empty<InvitacionEquipo>());
        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct) => Task.FromResult(false);
        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public HashSet<Guid> UsersWithActiveTeam { get; set; } = new();

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(UsersWithActiveTeam.Contains(userId));

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Equipo>>(TeamToReturn is null ? Array.Empty<Equipo>() : new[] { TeamToReturn });

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> AllUsers { get; set; } = new();

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(AllUsers);

        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(AllUsers.FirstOrDefault(u => u.UsuarioId == userId));

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(AllUsers.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
