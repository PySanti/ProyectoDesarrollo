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
    public DbSet<EventoHistorial> EventosHistorial => Set<EventoHistorial>();
    public DbSet<ParticipacionProyectada> Participaciones => Set<ParticipacionProyectada>();
    public DbSet<ConvocatoriaProyectada> Convocatorias => Set<ConvocatoriaProyectada>();

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

        modelBuilder.Entity<ParticipacionProyectada>(entity =>
        {
            entity.ToTable("participaciones_proyectadas");
            entity.HasKey(x => new { x.PartidaId, x.CompetidorId });
            entity.Property(x => x.PartidaId).HasColumnName("partidaid");
            entity.Property(x => x.CompetidorId).HasColumnName("competidorid");
            entity.Property(x => x.TipoCompetidor).HasColumnName("tipocompetidor").IsRequired();
            entity.HasIndex(x => x.CompetidorId).HasDatabaseName("ix_participaciones_proyectadas_competidorid");
        });

        modelBuilder.Entity<ConvocatoriaProyectada>(entity =>
        {
            entity.ToTable("convocatorias_proyectadas");
            entity.HasKey(x => x.ConvocatoriaId);
            entity.Property(x => x.ConvocatoriaId).HasColumnName("convocatoriaid").ValueGeneratedNever();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.Aceptada).HasColumnName("aceptada").IsRequired();
            entity.HasIndex(x => x.UsuarioId).HasDatabaseName("ix_convocatorias_proyectadas_usuarioid");
        });

        modelBuilder.Entity<EventoProcesado>(entity =>
        {
            entity.ToTable("eventos_procesados");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasColumnName("eventid").ValueGeneratedNever();
            entity.Property(x => x.EventType).HasColumnName("eventtype").IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurredat").IsRequired();
            entity.Property(x => x.ProcesadoAt).HasColumnName("procesadoat").IsRequired();
            entity.HasIndex(x => x.ProcesadoAt).HasDatabaseName("ix_eventos_procesados_procesadoat");
        });

        modelBuilder.Entity<EventoHistorial>(entity =>
        {
            entity.ToTable("eventos_historial");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.EventId).HasColumnName("eventid").IsRequired();
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.JuegoId).HasColumnName("juegoid");
            entity.Property(x => x.TipoEvento).HasColumnName("tipoevento").HasMaxLength(64).IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurredat").IsRequired();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid");
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            // jsonb es anotación relacional: Npgsql la aplica, InMemory la ignora.
            entity.Property(x => x.DetalleJson).HasColumnName("detalle").HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("ix_eventos_historial_eventid");
            entity.HasIndex(x => new { x.PartidaId, x.OccurredAt }).HasDatabaseName("ix_eventos_historial_partidaid_occurredat");
        });

        // SP-4b: token de concurrencia optimista sobre la columna de sistema xmin de PostgreSQL.
        // Solo aplica con Npgsql; el proveedor InMemory (dev/tests) no la tiene.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Marcador>().Property<uint>("xmin").IsRowVersion();
        }
    }
}
