// Infrastructure/Persistence/PartidasDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasDbContext : DbContext
{
    public PartidasDbContext(DbContextOptions<PartidasDbContext> options) : base(options)
    {
    }

    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<JuegoTrivia> JuegosTrivia => Set<JuegoTrivia>();
    public DbSet<JuegoBDT> JuegosBDT => Set<JuegoBDT>();

    private static readonly ValueConverter<PartidaId, Guid> PartidaIdConverter =
        new(v => v.Valor, v => PartidaId.From(v));
    private static readonly ValueConverter<JuegoId, Guid> JuegoIdConverter =
        new(v => v.Valor, v => JuegoId.From(v));
    private static readonly ValueConverter<NombrePartida, string> NombrePartidaConverter =
        new(v => v.Valor, v => NombrePartida.Crear(v));
    private static readonly ValueConverter<PuntajeAsignado, int> PuntajeConverter =
        new(v => v.Valor, v => PuntajeAsignado.Crear(v));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partida>(entity =>
        {
            entity.ToTable("partidas");
            entity.HasKey(x => x.PartidaId);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter);
            entity.Property(x => x.NombrePartida).HasColumnName("nombrepartida")
                .HasConversion(NombrePartidaConverter).IsRequired().HasMaxLength(NombrePartida.LongitudMaxima);
            entity.Property(x => x.Estado).HasColumnName("estado"); // nullable enum
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.ModoInicioPartida).HasColumnName("modoinicio").IsRequired();
            entity.Property(x => x.TiempoInicio).HasColumnName("tiempoinicio");
            entity.Property(x => x.MinimosParticipacion).HasColumnName("minimos").IsRequired();
            entity.Property(x => x.MaximosParticipacion).HasColumnName("maximos").IsRequired();
            entity.HasMany(x => x.Juegos).WithOne().HasForeignKey("partidaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Juegos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<JuegoReferencia>(entity =>
        {
            entity.ToTable("partida_juegos");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
        });

        modelBuilder.Entity<JuegoTrivia>(entity =>
        {
            entity.ToTable("juegos_trivia");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter).IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.HasMany(x => x.Preguntas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Preguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_trivia_partidaid");
        });

        modelBuilder.Entity<Pregunta>(entity =>
        {
            entity.ToTable("preguntas");
            entity.HasKey(x => x.PreguntaId);
            entity.Property(x => x.PreguntaId).HasColumnName("preguntaid").ValueGeneratedNever();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntaje").HasConversion(PuntajeConverter).IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimite").IsRequired();
            entity.HasMany(x => x.Opciones).WithOne().HasForeignKey("preguntaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Opciones).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Opcion>(entity =>
        {
            entity.ToTable("opciones");
            entity.HasKey(x => x.OpcionId);
            entity.Property(x => x.OpcionId).HasColumnName("opcionid").ValueGeneratedNever();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
        });

        modelBuilder.Entity<JuegoBDT>(entity =>
        {
            entity.ToTable("juegos_bdt");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter).IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.AreaBusqueda).HasColumnName("areabusqueda").IsRequired();
            entity.HasMany(x => x.Etapas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_bdt_partidaid");
        });

        modelBuilder.Entity<EtapaBDT>(entity =>
        {
            entity.ToTable("etapas_bdt");
            entity.HasKey(x => x.EtapaBDTId);
            entity.Property(x => x.EtapaBDTId).HasColumnName("etapabdtid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.CodigoQREsperado).HasColumnName("codigoqr").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntaje").HasConversion(PuntajeConverter).IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimite").IsRequired();
        });
    }
}
