using Microsoft.EntityFrameworkCore;
using Npgsql;
using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Exceptions;
using Umbral.TeamService.Domain.Entities;
using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.Infrastructure.Persistence;

public sealed class EquipoRepository : IEquipoRepository
{
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
}
