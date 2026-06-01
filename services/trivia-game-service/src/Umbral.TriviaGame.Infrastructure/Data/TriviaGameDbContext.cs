using Microsoft.EntityFrameworkCore;
using Umbral.TriviaGame.Domain.Entities;

namespace Umbral.TriviaGame.Infrastructure.Data;

public sealed class TriviaGameDbContext : DbContext
{
    public DbSet<TriviaForm> TriviaForms => Set<TriviaForm>();
    public DbSet<PartidaTrivia> PartidasTrivia => Set<PartidaTrivia>();
    public DbSet<TriviaInscripcion> TriviaInscripciones => Set<TriviaInscripcion>();
    public DbSet<RespuestaTrivia> RespuestasTrivia => Set<RespuestaTrivia>();

    public TriviaGameDbContext(DbContextOptions<TriviaGameDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TriviaGameDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
