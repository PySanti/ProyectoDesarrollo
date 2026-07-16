using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.IntegrationTests.Teams;

// Opt-in: requiere Postgres real. Correr con:
//   docker compose -f infra/docker-compose.yml up -d postgres
//   POSTGRES_TEST_CONNECTION="Host=localhost;Port=55432;Database=umbral_identity_tests;Username=umbral;Password=16102005" dotnet test ... --filter EquipoPostgresPersistenceTests
// Sin POSTGRES_TEST_CONNECTION los tests retornan sin assertar (skip suave, patrón SP-3i).
//
// El proveedor InMemory que usa EquipoPersistenceTests NO aplica índices únicos ni la
// nulabilidad real de la FK, así que no puede reproducir el slot de usuarioid que queda
// tomado por una fila huérfana. Estas pruebas necesitan el motor de verdad.
public sealed class EquipoPostgresPersistenceTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION");

    private static async Task<IdentityDbContext?> CreateFreshDbContextAsync()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var dbContext = new IdentityDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    /// <summary>
    /// Al salir del equipo, la fila de equipos_participantes debe BORRARSE, no quedar
    /// huérfana con equipoid = NULL: el índice único ux_equipos_participantes_usuarioid
    /// es global sobre usuarioid, así que una fila huérfana deja al usuario sin poder
    /// volver a entrar a ningún equipo.
    /// </summary>
    [Fact]
    public async Task Salir_del_equipo_borra_la_fila_y_no_deja_huerfana()
    {
        await using var dbContext = await CreateFreshDbContextAsync();
        if (dbContext is null)
        {
            return; // opt-in: sin Postgres configurado el test es un no-op (skip suave, patrón SP-3i)
        }

        var repository = new EquipoRepository(dbContext);
        var liderUserId = Guid.NewGuid();

        var equipo = Equipo.CrearPorParticipante("Equipo Viejo", liderUserId);
        await repository.AddAsync(equipo, CancellationToken.None);

        equipo.Salir(liderUserId);
        await repository.UpdateAsync(equipo, CancellationToken.None);

        var filas = await dbContext.ParticipantesEquipo.AsNoTracking().CountAsync();
        Assert.Equal(0, filas);
    }

    [Fact]
    public async Task Lider_puede_crear_equipo_nuevo_despues_de_salir_del_anterior()
    {
        await using var dbContext = await CreateFreshDbContextAsync();
        if (dbContext is null)
        {
            return;
        }

        var repository = new EquipoRepository(dbContext);
        var liderUserId = Guid.NewGuid();

        var equipoViejo = Equipo.CrearPorParticipante("Equipo Viejo", liderUserId);
        await repository.AddAsync(equipoViejo, CancellationToken.None);

        equipoViejo.Salir(liderUserId);
        await repository.UpdateAsync(equipoViejo, CancellationToken.None);

        var equipoNuevo = Equipo.CrearPorParticipante("Equipo Nuevo", liderUserId);
        await repository.AddAsync(equipoNuevo, CancellationToken.None);

        var persisted = await dbContext.Equipos
            .AsNoTracking()
            .Include(e => e.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipoNuevo.EquipoId);

        Assert.NotNull(persisted);
        Assert.Equal(EstadoEquipo.Activo, persisted.Estado);
        Assert.Equal(liderUserId, persisted.Participantes.Single().UsuarioId);
    }

    [Fact]
    public async Task Lider_puede_crear_equipo_nuevo_despues_de_eliminar_el_anterior()
    {
        await using var dbContext = await CreateFreshDbContextAsync();
        if (dbContext is null)
        {
            return;
        }

        var repository = new EquipoRepository(dbContext);
        var liderUserId = Guid.NewGuid();
        var miembroUserId = Guid.NewGuid();

        var equipoViejo = Equipo.CrearPorParticipante("Equipo Viejo", liderUserId);
        equipoViejo.AgregarParticipante(miembroUserId);
        await repository.AddAsync(equipoViejo, CancellationToken.None);

        equipoViejo.EliminarPorLider(liderUserId);
        await repository.UpdateAsync(equipoViejo, CancellationToken.None);

        // El equipo eliminado no debe dejar filas: ni del líder ni del miembro.
        var filas = await dbContext.ParticipantesEquipo.AsNoTracking().CountAsync();
        Assert.Equal(0, filas);

        var equipoNuevo = Equipo.CrearPorParticipante("Equipo Nuevo", liderUserId);
        await repository.AddAsync(equipoNuevo, CancellationToken.None);

        var miembroEnOtroEquipo = Equipo.CrearPorParticipante("Equipo Del Miembro", miembroUserId);
        await repository.AddAsync(miembroEnOtroEquipo, CancellationToken.None);

        Assert.True(await repository.ExistsActiveTeamByUserIdAsync(liderUserId, CancellationToken.None));
        Assert.True(await repository.ExistsActiveTeamByUserIdAsync(miembroUserId, CancellationToken.None));
    }
}
