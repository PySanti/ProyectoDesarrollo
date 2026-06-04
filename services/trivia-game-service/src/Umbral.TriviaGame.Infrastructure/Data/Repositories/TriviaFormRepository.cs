using Microsoft.EntityFrameworkCore;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Repositories;

internal sealed class TriviaFormRepository : ITriviaFormRepository
{
    private readonly TriviaGameDbContext _dbContext;

    public TriviaFormRepository(TriviaGameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TriviaForm form, CancellationToken cancellationToken = default)
    {
        await _dbContext.TriviaForms.AddAsync(form, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TriviaForm?> GetByIdAsync(TriviaFormId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaForms
            .Include(f => f.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(TriviaForm form, CancellationToken cancellationToken = default)
    {
        _dbContext.TriviaForms.Update(form);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TriviaForm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TriviaForms
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
