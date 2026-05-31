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

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios.AnyAsync(u => u.Correo == email, cancellationToken);
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
}
