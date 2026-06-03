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
    }
}
