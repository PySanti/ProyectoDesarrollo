using Microsoft.EntityFrameworkCore;
using Npgsql;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class EquipoRepository : IEquipoRepository
{
    private readonly IdentityDbContext _dbContext;

    public EquipoRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Equipos
            .AsNoTracking()
            .Include(x => x.Participantes)
            .OrderBy(e => e.NombreEquipo)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Equipos
            .AnyAsync(
                e => e.Estado == EstadoEquipo.Activo && e.Participantes.Any(p => p.UsuarioId == userId),
                cancellationToken);
    }

    public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Equipos
            .Include(x => x.Participantes)
            .FirstOrDefaultAsync(
                e => e.Estado == EstadoEquipo.Activo && e.Participantes.Any(p => p.UsuarioId == userId),
                cancellationToken);
    }

    public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
    {
        return _dbContext.Equipos
            .Include(x => x.Participantes)
            .FirstOrDefaultAsync(e => e.EquipoId == equipoId, cancellationToken);
    }

    public async Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Equipos.AddAsync(equipo, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException postgresException && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                if (string.Equals(postgresException.ConstraintName, "ux_equipos_participantes_usuarioid", StringComparison.OrdinalIgnoreCase))
                {
                    var creatorUserId = equipo.Participantes.FirstOrDefault()?.UsuarioId ?? Guid.Empty;
                    throw new ConcurrentTeamCreationException(creatorUserId);
                }
            }

            throw new PersistenceException("No fue posible persistir el equipo.", ex);
        }
    }

    public async Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
    {
        try
        {
            var equipoEntry = _dbContext.Entry(equipo);
            if (equipoEntry.State == EntityState.Detached)
            {
                _dbContext.Equipos.Attach(equipo);
            }

            // Los participantes que salieron de la coleccion los borra EF solo: la relacion es
            // requerida (ver IdentityDbContext), asi que el huerfano se marca Deleted en vez de
            // quedarse con equipoid = NULL. Aca solo queda resolver el alta de los nuevos.
            var persistedParticipanteIds = (await _dbContext.ParticipantesEquipo
                    .AsNoTracking()
                    .Where(p => EF.Property<Guid>(p, "equipoid") == equipo.EquipoId)
                    .Select(p => p.ParticipanteEquipoId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            foreach (var participante in equipo.Participantes)
            {
                var participanteEntry = _dbContext.Entry(participante);
                if (participanteEntry.State == EntityState.Added)
                {
                    continue;
                }

                if (!persistedParticipanteIds.Contains(participante.ParticipanteEquipoId))
                {
                    participanteEntry.State = EntityState.Added;
                }
                else if (participanteEntry.State == EntityState.Detached)
                {
                    participanteEntry.State = EntityState.Modified;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException postgresException && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                if (string.Equals(postgresException.ConstraintName, "ux_equipos_participantes_usuarioid", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UniqueMembershipConflictException("El participante ya pertenece a un equipo activo.", ex);
                }
            }

            throw new PersistenceException("No fue posible persistir los cambios del equipo.", ex);
        }
    }
}
