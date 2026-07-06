using Microsoft.EntityFrameworkCore;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Infrastructure.Persistence;

public sealed class PuntuacionesDbContext : DbContext
{
    public PuntuacionesDbContext(DbContextOptions<PuntuacionesDbContext> options) : base(options)
    {
    }

    public DbSet<PartidaProyectada> Partidas => Set<PartidaProyectada>();
    public DbSet<JuegoProyectado> Juegos => Set<JuegoProyectado>();
    public DbSet<Marcador> Marcadores => Set<Marcador>();
    public DbSet<EventoProcesado> EventosProcesados => Set<EventoProcesado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartidaProyectada>(entity =>
        {
            entity.ToTable("partidas_proyectadas");
            entity.HasKey(x => x.PartidaId);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").ValueGeneratedNever();
            entity.Property(x => x.SesionPartidaId).HasColumnName("sesionpartidaid").IsRequired();
            entity.Property(x => x.Modalidad).HasColumnName("modalidad");
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaInicio).HasColumnName("fechainicio");
            entity.Property(x => x.FechaFin).HasColumnName("fechafin");
        });

        modelBuilder.Entity<JuegoProyectado>(entity =>
        {
            entity.ToTable("juegos_proyectados");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").ValueGeneratedNever();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_proyectados_partidaid");
        });

        modelBuilder.Entity<Marcador>(entity =>
        {
            entity.ToTable("marcadores");
            entity.HasKey(x => new { x.JuegoId, x.CompetidorId });
            entity.Property(x => x.JuegoId).HasColumnName("juegoid");
            entity.Property(x => x.CompetidorId).HasColumnName("competidorid");
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.TipoCompetidor).HasColumnName("tipocompetidor").IsRequired();
            entity.Property(x => x.PuntosAcumulados).HasColumnName("puntosacumulados").IsRequired();
            entity.Property(x => x.TiempoAcumuladoMs).HasColumnName("tiempoacumuladoms").IsRequired();
            entity.Property(x => x.UnidadesGanadas).HasColumnName("unidadesganadas").IsRequired();
            entity.HasIndex(x => x.JuegoId).HasDatabaseName("ix_marcadores_juegoid");
        });

        modelBuilder.Entity<EventoProcesado>(entity =>
        {
            entity.ToTable("eventos_procesados");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasColumnName("eventid").ValueGeneratedNever();
            entity.Property(x => x.EventType).HasColumnName("eventtype").IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurredat").IsRequired();
            entity.Property(x => x.ProcesadoAt).HasColumnName("procesadoat").IsRequired();
        });

        // SP-4b: token de concurrencia optimista sobre la columna de sistema xmin de PostgreSQL.
        // Solo aplica con Npgsql; el proveedor InMemory (dev/tests) no la tiene.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Marcador>().UseXminAsConcurrencyToken();
        }
    }
}
