using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionDbContext : DbContext
{
    public OperacionesSesionDbContext(DbContextOptions<OperacionesSesionDbContext> options) : base(options)
    {
    }

    public DbSet<SesionPartida> Sesiones => Set<SesionPartida>();

    private static readonly ValueConverter<SesionPartidaId, Guid> SesionPartidaIdConverter =
        new(v => v.Valor, v => SesionPartidaId.From(v));
    private static readonly ValueConverter<InscripcionId, Guid> InscripcionIdConverter =
        new(v => v.Valor, v => InscripcionId.From(v));
    private static readonly ValueConverter<ConvocatoriaId, Guid> ConvocatoriaIdConverter =
        new(v => v.Valor, v => ConvocatoriaId.From(v));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SesionPartida>(entity =>
        {
            entity.ToTable("sesiones_partida");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(SesionPartidaIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.ModoInicioPartida).HasColumnName("modoinicio").IsRequired();
            entity.Property(x => x.TiempoInicio).HasColumnName("tiempoinicio");
            entity.Property(x => x.MinimosParticipacion).HasColumnName("minimos").IsRequired();
            entity.Property(x => x.MaximosParticipacion).HasColumnName("maximos").IsRequired();
            entity.Property(x => x.FechaInicio).HasColumnName("fechainicio");
            entity.Property(x => x.FechaFin).HasColumnName("fechafin");
            entity.HasIndex(x => x.PartidaId).IsUnique().HasDatabaseName("ix_sesiones_partidaid");
            entity.HasMany(x => x.Juegos).WithOne().HasForeignKey("sesionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Juegos).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Inscripciones).WithOne().HasForeignKey("sesionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Inscripciones).UsePropertyAccessMode(PropertyAccessMode.Field);
            if (Database.IsNpgsql())
            {
                entity.Property<uint>("xmin")
                    .HasColumnType("xid")
                    .IsRowVersion();
            }
        });

        modelBuilder.Entity<JuegoResumen>(entity =>
        {
            entity.ToTable("sesion_juegos");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadojuego").IsRequired();
            entity.Property(x => x.AreaBusqueda).HasColumnName("areabusqueda").IsRequired();
            entity.HasMany(x => x.Preguntas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Preguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Etapas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<EtapaSnapshot>(entity =>
        {
            entity.ToTable("etapas_snapshot");
            entity.HasKey(x => x.EtapaId);
            entity.Property(x => x.EtapaId).HasColumnName("etapaid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.CodigoQREsperado).HasColumnName("codigoqresperado").IsRequired();
            entity.Property(x => x.Puntaje).HasColumnName("puntaje").IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimitesegundos").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadoetapa").IsRequired();
            entity.Property(x => x.FechaActivacion).HasColumnName("fechaactivacion");
            entity.Property(x => x.FechaCierre).HasColumnName("fechacierre");
            entity.Property(x => x.MotivoCierre).HasColumnName("motivocierre");
            entity.Property(x => x.GanadorParticipanteId).HasColumnName("ganadorparticipanteid");
            entity.Property(x => x.GanadorEquipoId).HasColumnName("ganadorequipoid");
            entity.Property(x => x.TiempoResolucionMs).HasColumnName("tiemporesolucionms");
            entity.HasMany(x => x.Tesoros).WithOne().HasForeignKey("etapaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Tesoros).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TesoroQR>(entity =>
        {
            entity.ToTable("tesoros_qr");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            entity.Property(x => x.QrDecodificado).HasColumnName("qrdecodificado");
            entity.Property(x => x.Resultado).HasColumnName("resultado").IsRequired();
            entity.Property(x => x.FechaEnvio).HasColumnName("fechaenvio").IsRequired();
        });

        modelBuilder.Entity<PreguntaSnapshot>(entity =>
        {
            entity.ToTable("preguntas_snapshot");
            entity.HasKey(x => x.PreguntaId);
            entity.Property(x => x.PreguntaId).HasColumnName("preguntaid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntajeasignado").IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimitesegundos").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadopregunta").IsRequired();
            entity.Property(x => x.FechaActivacion).HasColumnName("fechaactivacion");
            entity.Property(x => x.FechaCierre).HasColumnName("fechacierre");
            entity.Property(x => x.MotivoCierre).HasColumnName("motivocierre");
            entity.Property(x => x.GanadorParticipanteId).HasColumnName("ganadorparticipanteid");
            entity.Property(x => x.GanadorEquipoId).HasColumnName("ganadorequipoid");
            entity.HasMany(x => x.Opciones).WithOne().HasForeignKey("preguntaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Opciones).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Respuestas).WithOne().HasForeignKey("preguntaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Respuestas).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OpcionSnapshot>(entity =>
        {
            entity.ToTable("opciones_snapshot");
            entity.HasKey(x => x.OpcionId);
            entity.Property(x => x.OpcionId).HasColumnName("opcionid").ValueGeneratedNever();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
        });

        modelBuilder.Entity<RespuestaTrivia>(entity =>
        {
            entity.ToTable("respuestas_trivia");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.OpcionId).HasColumnName("opcionid").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
            entity.Property(x => x.Instante).HasColumnName("instante").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
        });

        modelBuilder.Entity<InscripcionPartida>(entity =>
        {
            entity.ToTable("inscripciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(InscripcionIdConverter);
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaInscripcion).HasColumnName("fechainscripcion").IsRequired();
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
            // Guid.Empty en Individual y en filas preexistentes: sin lider registrado no hay
            // auto-aceptado, que es el default seguro (ver InscripcionPartida.Aceptar).
            entity.Property(x => x.LiderId).HasColumnName("liderid").IsRequired()
                .HasDefaultValue(Guid.Empty);
            // HU-19: snapshot de miembros para diferir la creación de convocatorias hasta la
            // aceptación del operador. Colección primitiva (jsonb en Npgsql). Se mapea el campo
            // mutable _miembrosSnapshot (List<Guid>) — EF 8 no admite la vista de solo lectura
            // MiembrosSnapshot (IReadOnlyList) como colección primitiva.
            entity.PrimitiveCollection<List<Guid>>("_miembrosSnapshot")
                .HasColumnName("miembrossnapshot")
                .HasDefaultValue(new List<Guid>()) // array vacío para filas Individual y filas preexistentes
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Convocatorias).WithOne().HasForeignKey("inscripcionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Convocatorias).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Convocatoria>(entity =>
        {
            entity.ToTable("convocatorias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(ConvocatoriaIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.EquipoId).HasColumnName("equipoid").IsRequired();
            entity.Property(x => x.UsuarioId).HasColumnName("usuarioid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaEnvio).HasColumnName("fechaenvio").IsRequired();
            entity.Property(x => x.FechaRespuesta).HasColumnName("fecharespuesta");
        });
    }
}
