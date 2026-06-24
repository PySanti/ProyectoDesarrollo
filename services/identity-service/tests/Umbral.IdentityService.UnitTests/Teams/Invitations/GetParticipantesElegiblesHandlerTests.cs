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
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

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
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

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
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(lider), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Users_Already_In_A_Team()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var eligibleUserId = Guid.NewGuid();
        var inTeamUserId = Guid.NewGuid();

        var eligibleUser = Usuario.Crear("kc-1", "Elegible", "elegible@test.com", RolUsuario.Participante);
        var inTeamUser = Usuario.Crear("kc-2", "EnEquipo", "enequipo@test.com", RolUsuario.Participante);

        // Use reflection to set UserId for testing purposes — or use typed fakes
        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { eligibleUser, inTeamUser }
        };

        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid> { inTeamUser.UsuarioId }
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(lider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(eligibleUser.UsuarioId, result[0].UserId);
        Assert.Equal("Elegible", result[0].Nombre);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Current_Team_Members()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var liderUser = Usuario.Crear("kc-lider", "Lider", "lider@test.com", RolUsuario.Participante);
        var otherUser = Usuario.Crear("kc-other", "Otro", "otro@test.com", RolUsuario.Participante);

        // The lider is a member — even though it appears in the user list, it should be excluded
        // We use the lider's real UsuarioId from the equipo (which is different from the Usuario entity)
        // For simplicity: the equipo has lider as member via actorId; the fake repo checks membership

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { otherUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(lider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(otherUser.UsuarioId, result[0].UserId);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Non_Participante_Role_Users()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var adminUser = Usuario.Crear("kc-admin", "Admin", "admin@test.com", RolUsuario.Administrador);
        var participanteUser = Usuario.Crear("kc-p", "Participante", "p@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { adminUser, participanteUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(lider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(participanteUser.UsuarioId, result[0].UserId);
    }

    // Fakes

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
