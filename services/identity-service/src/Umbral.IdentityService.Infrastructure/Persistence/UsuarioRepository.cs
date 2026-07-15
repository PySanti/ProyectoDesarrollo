using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
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

    public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
    {
        // Usuario.KeycloakId se persiste como string (el id que devuelve Keycloak, un UUID en
        // formato canónico minúsculas-con-guiones: ver KeycloakIdentityAdapter.CreateUserAsync,
        // que lo toma tal cual del header Location de la API admin de Keycloak). Guid.ToString()
        // por defecto ("D") produce ese mismo formato, pero se compara en minúsculas por ambos
        // lados para no depender de que Keycloak nunca cambie el casing.
        var keycloakIdNormalizado = keycloakId.ToString().ToLowerInvariant();
        return _dbContext.Usuarios.FirstOrDefaultAsync(
            u => u.KeycloakId.ToLower() == keycloakIdNormalizado,
            cancellationToken);
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
