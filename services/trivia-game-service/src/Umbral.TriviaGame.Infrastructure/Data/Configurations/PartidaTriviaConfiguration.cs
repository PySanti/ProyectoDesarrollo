using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Configurations;

internal sealed class PartidaTriviaConfiguration : IEntityTypeConfiguration<PartidaTrivia>
{
    public void Configure(EntityTypeBuilder<PartidaTrivia> builder)
    {
        builder.ToTable("partidas_trivia");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(ValueConverters.PartidaIdConverter);

        builder.Property(p => p.Nombre)
            .HasConversion(ValueConverters.NombrePartidaConverter)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Modalidad)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ModoInicio)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.FormularioAsociadoId)
            .HasConversion(ValueConverters.TriviaFormIdConverter)
            .IsRequired();

        builder.Property(p => p.CreatedByOperatorId)
            .HasConversion(ValueConverters.OperatorIdConverter)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.TiempoInicio)
            .HasConversion(ValueConverters.TiempoInicioConverter)
            .IsRequired();

        builder.Property(p => p.MinimoParticipantes)
            .HasConversion(ValueConverters.CantidadMinimaConverter)
            .IsRequired();

#pragma warning disable CS8620
        builder.Property(p => p.MaximoJugadores)
            .HasConversion(ValueConverters.CantidadMaximaJugadoresConverter);

        builder.Property(p => p.MaximoEquipos)
            .HasConversion(ValueConverters.CantidadMaximaEquiposConverter);

        builder.Property(p => p.MinimoJugadoresPorEquipo)
            .HasConversion(ValueConverters.JugadoresPorEquipoMinConverter);

        builder.Property(p => p.MaximoJugadoresPorEquipo)
            .HasConversion(ValueConverters.JugadoresPorEquipoMaxConverter);
#pragma warning restore CS8620

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.StartedAtUtc);

        builder.Property(p => p.PreguntaActualId)
            .HasConversion(ValueConverters.QuestionIdConverter);

        builder.Property(p => p.PreguntaAbiertaEnUtc);

        builder.HasMany(p => p.Respuestas)
            .WithOne()
            .HasForeignKey(r => r.PartidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
        builder.Metadata.FindNavigation(nameof(PartidaTrivia.Respuestas))!
            .SetField("_respuestas");
    }
}
