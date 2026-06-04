using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Domain.Entities;
using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.Infrastructure.Persistence;

public sealed class EquipoRepository : IEquipoRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InMemoryLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly TeamDbContext _dbContext;

    public EquipoRepository(TeamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Equipos
            .AnyAsync(
                e => e.Estado == EstadoEquipo.Activo && e.Participantes.Any(p => p.UsuarioId == userId),
                cancellationToken);
    }

    public Task<bool> ExistsByAccessCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Equipos.AnyAsync(e => e.CodigoAcceso == normalizedCode, cancellationToken);
    }

    public Task<Equipo?> GetActiveByAccessCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Equipos
            .Include(x => x.Participantes)
            .FirstOrDefaultAsync(
                e => e.Estado == EstadoEquipo.Activo && e.CodigoAcceso == normalizedCode,
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

                if (string.Equals(postgresException.ConstraintName, "ux_equipos_codigoacceso", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AccessCodeGenerationException("Colision concurrente de codigo de acceso. Reintentar generacion.");
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

            foreach (var participante in equipo.Participantes)
            {
                var exists = await _dbContext.ParticipantesEquipo
                    .AnyAsync(x => x.ParticipanteEquipoId == participante.ParticipanteEquipoId, cancellationToken);

                var participanteEntry = _dbContext.Entry(participante);
                participanteEntry.State = exists ? EntityState.Modified : EntityState.Added;
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

                if (string.Equals(postgresException.ConstraintName, "ux_equipos_codigoacceso", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AccessCodeGenerationException("Colision concurrente de codigo de acceso. Reintentar generacion.");
                }
            }

            throw new PersistenceException("No fue posible persistir los cambios del equipo.", ex);
        }
    }

    public async Task<T> ExecuteWithAccessCodeLockAsync<T>(string teamCode, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var normalizedCode = teamCode.Trim().ToUpperInvariant();
            var semaphore = InMemoryLocks.GetOrAdd(normalizedCode, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            await AcquireAdvisoryLockAsync(teamCode, cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task AcquireAdvisoryLockAsync(string teamCode, CancellationToken cancellationToken)
    {
        var lockKey = CreateStableLockKey(teamCode);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(@lockKey)",
            new[] { new NpgsqlParameter("@lockKey", lockKey) },
            cancellationToken);
    }

    private static long CreateStableLockKey(string teamCode)
    {
        var normalizedCode = teamCode.Trim().ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode));
        return BitConverter.ToInt64(bytes, 0);
    }
}
