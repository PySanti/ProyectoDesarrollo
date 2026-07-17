using Umbral.IdentityService.Domain.ValueObjects;
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

    // Los tres tests que siguen fijan el espacio de ids. ParticipanteEquipo.SubjectId guarda el
    // sub de Keycloak, no el UsuarioId local (mismo patron que ListarEquiposQueryHandler y
    // ResolverNombresQueryHandler). Por eso los fixtures usan un Guid real como KeycloakId: un
    // "kc-1" no parseable no se parece a nada que Keycloak emita y esconde justo este bug.

    [Fact]
    public async Task GetElegibles_Devuelve_El_Sub_De_Keycloak_Y_No_El_UsuarioId_Local()
    {
        var subLider = Guid.NewGuid();
        var subInvitable = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);

        var invitable = Usuario.Crear(subInvitable.ToString(), "Invitable", "i@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository { AllUsers = new List<Usuario> { invitable } };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        // El id devuelto es con el que se creara la invitacion, y con el que el invitado la
        // buscara desde su token. Si aqui sale el UsuarioId local, la invitacion queda archivada
        // bajo un id que el invitado nunca presenta y no la ve nunca.
        var item = Assert.Single(result);
        Assert.Equal(subInvitable, item.UserId);
        Assert.NotEqual(invitable.UsuarioId.Valor, item.UserId);
    }

    [Fact]
    public async Task GetElegibles_Excluye_Al_Propio_Lider()
    {
        var subLider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);

        // El lider si esta en la lista de usuarios: es un participante mas. Excluirlo es trabajo
        // del handler, no del fixture.
        var liderUser = Usuario.Crear(subLider.ToString(), "Lider", "lider@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository { AllUsers = new List<Usuario> { liderUser } };
        var equipoRepo = new FakeEquipoRepository { TeamToReturn = equipo };
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetElegibles_Excluye_A_Quien_Ya_Tiene_Equipo_Por_Su_Sub()
    {
        var subLider = Guid.NewGuid();
        var subConEquipo = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);

        var conEquipo = Usuario.Crear(subConEquipo.ToString(), "YaTieneEquipo", "y@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository { AllUsers = new List<Usuario> { conEquipo } };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            // El repositorio real indexa la membresia por sub: si el handler pregunta con el
            // UsuarioId local, este guarda nunca dispara y se puede invitar a quien ya tiene equipo.
            UsersWithActiveTeam = new HashSet<Guid> { subConEquipo }
        };
        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Users_Already_In_A_Team()
    {
        var subLider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);

        var subElegible = Guid.NewGuid();
        var subEnEquipo = Guid.NewGuid();

        var eligibleUser = Usuario.Crear(subElegible.ToString(), "Elegible", "elegible@test.com", RolUsuario.Participante);
        var inTeamUser = Usuario.Crear(subEnEquipo.ToString(), "EnEquipo", "enequipo@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { eligibleUser, inTeamUser }
        };

        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid> { subEnEquipo }
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(subElegible, result[0].UserId);
        Assert.Equal("Elegible", result[0].Nombre);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Current_Team_Members()
    {
        var subLider = Guid.NewGuid();
        var subMiembro = Guid.NewGuid();
        var subLibre = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);
        equipo.AgregarParticipante(subMiembro);

        // Los tres estan en la lista de usuarios. Excluir a los dos que ya son del equipo es
        // trabajo del handler: el fixture no le hace el favor de omitirlos.
        var liderUser = Usuario.Crear(subLider.ToString(), "Lider", "lider@test.com", RolUsuario.Participante);
        var miembroUser = Usuario.Crear(subMiembro.ToString(), "Miembro", "miembro@test.com", RolUsuario.Participante);
        var libreUser = Usuario.Crear(subLibre.ToString(), "Libre", "libre@test.com", RolUsuario.Participante);

        var usuarioRepo = new FakeUsuarioRepository
        {
            AllUsers = new List<Usuario> { liderUser, miembroUser, libreUser }
        };
        var equipoRepo = new FakeEquipoRepository
        {
            TeamToReturn = equipo,
            UsersWithActiveTeam = new HashSet<Guid>()
        };

        var handler = new GetParticipantesElegiblesQueryHandler(equipoRepo, usuarioRepo);

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(subLibre, result[0].UserId);
    }

    [Fact]
    public async Task GetElegibles_Excludes_Non_Participante_Role_Users()
    {
        var subLider = Guid.NewGuid();
        var subAdmin = Guid.NewGuid();
        var subParticipante = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", subLider);

        var adminUser = Usuario.Crear(subAdmin.ToString(), "Admin", "admin@test.com", RolUsuario.Administrador);
        var participanteUser = Usuario.Crear(subParticipante.ToString(), "Participante", "p@test.com", RolUsuario.Participante);

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

        var result = await handler.Handle(new GetParticipantesElegiblesQuery(subLider), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(subParticipante, result[0].UserId);
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

        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(AllUsers.FirstOrDefault(u => u.UsuarioId == userId));

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(AllUsers.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
