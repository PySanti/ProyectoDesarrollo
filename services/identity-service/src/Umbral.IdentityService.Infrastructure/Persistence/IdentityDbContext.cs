using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(x => x.UsuarioId);
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid");
            entity.Property(x => x.KeycloakId).HasColumnName("keycloakid").IsRequired().HasMaxLength(128);
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired().HasMaxLength(120);
            entity.Property(x => x.Correo).HasColumnName("correo").IsRequired().HasMaxLength(320);
            entity.HasIndex(x => x.Correo).IsUnique();
            entity.Property(x => x.Rol).HasColumnName("rol").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
        });
    }
}
