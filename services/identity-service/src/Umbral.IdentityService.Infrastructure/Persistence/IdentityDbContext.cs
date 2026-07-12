using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<ParticipanteEquipo> ParticipantesEquipo => Set<ParticipanteEquipo>();
    public DbSet<InvitacionEquipo> InvitacionesEquipo => Set<InvitacionEquipo>();
    public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();
    public DbSet<HistorialNombreEquipo> HistorialNombresEquipo => Set<HistorialNombreEquipo>();
    public DbSet<ParticipacionActivaEquipo> ParticipacionesActivasEquipo => Set<ParticipacionActivaEquipo>();

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

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.ToTable("equipos");
            entity.HasKey(x => x.EquipoId);
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.Property(x => x.NombreEquipo).HasColumnName("nombreequipo").HasMaxLength(120).IsRequired();
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

        modelBuilder.Entity<InvitacionEquipo>(entity =>
        {
            entity.ToTable("invitaciones_equipo");
            entity.HasKey(x => x.InvitacionEquipoId);
            entity.Property(x => x.InvitacionEquipoId).HasColumnName("invitacionequipoid");
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.InvitadoUserId).HasColumnName("invitadouserid").IsRequired();
            entity.Property(x => x.InvitadoPorUserId).HasColumnName("invitadoporuserid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaCreacionUtc).HasColumnName("fechacreacionutc").IsRequired();
            entity.HasIndex(x => x.InvitadoUserId)
                .HasDatabaseName("ix_invitaciones_equipo_invitadouserid");
            entity.HasOne<Equipo>()
                .WithMany()
                .HasForeignKey(x => x.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PermisoRol>(entity =>
        {
            entity.ToTable("permisos_rol");
            entity.HasKey(p => new { p.Rol, p.Permiso });
            entity.Property(p => p.Rol).HasColumnName("rol");
            entity.Property(p => p.Permiso).HasColumnName("permiso");
        });

        modelBuilder.Entity<HistorialNombreEquipo>(entity =>
        {
            entity.ToTable("historial_nombre_equipo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.NombreEquipo).HasColumnName("nombreequipo").HasMaxLength(120).IsRequired();
            entity.Property(x => x.FechaRegistroUtc).HasColumnName("fecharegistroutc").IsRequired();
            entity.HasIndex(x => x.UsuarioId).HasDatabaseName("ix_historial_nombre_equipo_usuarioid");
        });

        modelBuilder.Entity<ParticipacionActivaEquipo>(entity =>
        {
            entity.ToTable("participaciones_activas_equipo");
            entity.HasKey(x => new { x.EquipoId, x.PartidaId });
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.Property(x => x.PartidaId).HasColumnName("partidaid");
            entity.Property(x => x.FechaRegistroUtc).HasColumnName("fecharegistroutc").IsRequired();
        });
    }
}
