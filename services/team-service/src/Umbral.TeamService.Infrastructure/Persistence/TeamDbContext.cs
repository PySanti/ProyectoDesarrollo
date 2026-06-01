using Microsoft.EntityFrameworkCore;
using Umbral.TeamService.Domain.Entities;

namespace Umbral.TeamService.Infrastructure.Persistence;

public sealed class TeamDbContext : DbContext
{
    public TeamDbContext(DbContextOptions<TeamDbContext> options)
        : base(options)
    {
    }

    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<ParticipanteEquipo> ParticipantesEquipo => Set<ParticipanteEquipo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.ToTable("equipos");
            entity.HasKey(x => x.EquipoId);
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.Property(x => x.NombreEquipo).HasColumnName("nombreequipo").HasMaxLength(120).IsRequired();
            entity.Property(x => x.CodigoAcceso).HasColumnName("codigoacceso").HasMaxLength(16).IsRequired();
            entity.HasIndex(x => x.CodigoAcceso)
                .HasDatabaseName("ux_equipos_codigoacceso")
                .IsUnique();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.HasMany(x => x.Participantes)
                .WithOne()
                .HasForeignKey("equipoid")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParticipanteEquipo>(entity =>
        {
            entity.ToTable("equipos_participantes");
            entity.HasKey(x => x.ParticipanteEquipoId);
            entity.Property(x => x.ParticipanteEquipoId).HasColumnName("participanteequipoid");
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.FechaUnionUtc).HasColumnName("fechaunionutc").IsRequired();
            entity.Property(x => x.EsLider).HasColumnName("eslider").IsRequired();
            entity.HasIndex(x => x.UsuarioId)
                .HasDatabaseName("ux_equipos_participantes_usuarioid")
                .IsUnique();
        });
    }
}
