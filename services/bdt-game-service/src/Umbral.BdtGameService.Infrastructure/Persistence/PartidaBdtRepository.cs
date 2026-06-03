using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Domain.Entities;

namespace Umbral.BdtGameService.Infrastructure.Persistence;

public sealed class PartidaBdtRepository : IPartidaBdtRepository
{
    private readonly BdtDbContext _dbContext;

    public PartidaBdtRepository(BdtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PartidaBDT partida, CancellationToken cancellationToken)
    {
        await _dbContext.Partidas.AddAsync(partida, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteWithPartidaRegistrationLockAsync<TResult>(
        Guid partidaId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return await operation(cancellationToken);
        }

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (IsNpgsqlProvider())
            {
                await AcquirePostgresPartidaLockAsync(partidaId, cancellationToken);
            }

            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public async Task<PartidaBDT?> GetByIdWithExploradoresAsync(Guid partidaId, CancellationToken cancellationToken)
    {
        return await _dbContext.Partidas
            .Include(partida => partida.Exploradores)
            .Include(partida => partida.Etapas)
            .SingleOrDefaultAsync(partida => partida.PartidaId == partidaId, cancellationToken);
    }

    public async Task UpdateAsync(PartidaBDT partida, CancellationToken cancellationToken)
    {
        try
        {
            var existingExplorerIds = await _dbContext.Set<ExploradorBDT>()
                .AsNoTracking()
                .Where(explorador => explorador.PartidaId == partida.PartidaId)
                .Select(explorador => explorador.ExploradorId)
                .ToListAsync(cancellationToken);

            foreach (var entry in _dbContext.ChangeTracker.Entries<ExploradorBDT>())
            {
                if (entry.State == EntityState.Modified && !existingExplorerIds.Contains(entry.Entity.ExploradorId))
                {
                    entry.State = EntityState.Added;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateExplorerConflict(ex))
        {
            throw new InvalidOperationException("El participante ya esta inscrito en esta BDT.", ex);
        }
    }

    private static bool IsDuplicateExplorerConflict(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("ux_exploradores_bdt_partida_competidor_tipo", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsNpgsqlProvider()
    {
        return _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task AcquirePostgresPartidaLockAsync(Guid partidaId, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(@lockKey);";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockKey";
        parameter.Value = CreateAdvisoryLockKey(partidaId);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static long CreateAdvisoryLockKey(Guid partidaId)
    {
        return BitConverter.ToInt64(partidaId.ToByteArray(), 0);
    }
}
