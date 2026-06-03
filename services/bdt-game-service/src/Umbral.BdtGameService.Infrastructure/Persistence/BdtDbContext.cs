using Microsoft.EntityFrameworkCore;
using Umbral.BdtGameService.Domain.Entities;

namespace Umbral.BdtGameService.Infrastructure.Persistence;

public sealed class BdtDbContext : DbContext
{
    public BdtDbContext(DbContextOptions<BdtDbContext> options)
        : base(options)
    {
    }

    public DbSet<PartidaBDT> Partidas => Set<PartidaBDT>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartidaBDT>(entity =>
        {
            entity.ToTable("partidas_bdt");
            entity.HasKey(partida => partida.PartidaId);
            entity.Property(partida => partida.PartidaId).HasColumnName("partida_id");
            entity.Property(partida => partida.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            entity.Property(partida => partida.Modalidad).HasColumnName("modalidad").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(partida => partida.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(partida => partida.MinimoParticipantes).HasColumnName("minimo_participantes").IsRequired();
            entity.Property(partida => partida.MaximoParticipantes).HasColumnName("maximo_participantes");
            entity.Property(partida => partida.MaximoEquipos).HasColumnName("maximo_equipos");
            entity.Property(partida => partida.MinimoJugadoresPorEquipo).HasColumnName("minimo_jugadores_por_equipo");
            entity.Property(partida => partida.ModoInicio).HasColumnName("modo_inicio").HasConversion<string>().HasMaxLength(30).IsRequired();

            entity.OwnsOne(partida => partida.AreaBusqueda, area =>
            {
                area.Property(value => value.Descripcion)
                    .HasColumnName("area_busqueda")
                    .HasMaxLength(500)
                    .IsRequired();
            });

            entity.HasMany(partida => partida.Etapas)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(partida => partida.Exploradores)
                .WithOne()
                .HasForeignKey(explorador => explorador.PartidaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EtapaBDT>(entity =>
        {
            entity.ToTable("etapas_bdt");
            entity.HasKey(etapa => etapa.EtapaId);
            entity.Property(etapa => etapa.EtapaId).HasColumnName("etapa_id");
            entity.Property(etapa => etapa.Orden).HasColumnName("orden").IsRequired();
            entity.Property(etapa => etapa.CodigoQREsperado).HasColumnName("codigo_qr_esperado").HasMaxLength(250).IsRequired();
            entity.Property(etapa => etapa.TiempoLimiteSegundos).HasColumnName("tiempo_limite_segundos").IsRequired();
        });

        modelBuilder.Entity<ExploradorBDT>(entity =>
        {
            entity.ToTable("exploradores_bdt");
            entity.HasKey(explorador => explorador.ExploradorId);
            entity.Property(explorador => explorador.ExploradorId).HasColumnName("explorador_id");
            entity.Property(explorador => explorador.PartidaId).HasColumnName("partida_id").IsRequired();
            entity.Property(explorador => explorador.CompetidorId).HasColumnName("competidor_id").IsRequired();
            entity.Property(explorador => explorador.TipoCompetidor).HasColumnName("tipo_competidor").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(explorador => explorador.FechaInscripcionUtc).HasColumnName("fecha_inscripcion_utc").IsRequired();
            entity.Property(explorador => explorador.EtapasGanadas).HasColumnName("etapas_ganadas").IsRequired();
            entity.Property(explorador => explorador.TiempoAcumuladoEtapasGanadasSegundos).HasColumnName("tiempo_acumulado_etapas_ganadas_segundos").IsRequired();
            entity.HasIndex(explorador => new { explorador.PartidaId, explorador.CompetidorId, explorador.TipoCompetidor })
                .IsUnique()
                .HasDatabaseName("ux_exploradores_bdt_partida_competidor_tipo");
        });
    }
}
