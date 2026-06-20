using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly IdentityDbContext _dbContext;

    public UsuarioRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == userId, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Usuarios.Where(u => u.Correo == email);

        if (excludingUserId.HasValue)
        {
            query = query.Where(u => u.UsuarioId != excludingUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Usuarios.AddAsync(usuario, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("Failed to persist usuario", ex);
        }
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.Usuarios.Update(usuario);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("Failed to persist usuario", ex);
        }
    }

    public async Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.Usuarios.Remove(usuario);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("Failed to remove usuario", ex);
        }
    }
}
