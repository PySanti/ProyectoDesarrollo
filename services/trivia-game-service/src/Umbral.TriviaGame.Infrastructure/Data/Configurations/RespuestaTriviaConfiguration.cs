using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Configurations;

internal sealed class RespuestaTriviaConfiguration : IEntityTypeConfiguration<RespuestaTrivia>
{
    public void Configure(EntityTypeBuilder<RespuestaTrivia> builder)
    {
        builder.ToTable("respuestas_trivia");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(ValueConverters.RespuestaTriviaIdConverter);

        builder.Property(r => r.PartidaId)
            .HasConversion(ValueConverters.PartidaIdConverter)
            .IsRequired();

        builder.Property(r => r.PreguntaId)
            .HasConversion(ValueConverters.QuestionIdConverter)
            .IsRequired();

        builder.Property(r => r.UsuarioId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.OpcionSeleccionadaIndex)
            .IsRequired();

        builder.Property(r => r.EsCorrecta)
            .IsRequired();

        builder.Property(r => r.PuntajeObtenido)
            .IsRequired();

        builder.Property(r => r.FechaRespuesta)
            .IsRequired();

        builder.HasIndex(r => new { r.PartidaId, r.UsuarioId, r.PreguntaId })
            .IsUnique()
            .HasDatabaseName("IX_respuestas_trivia_partida_usuario_pregunta");
    }
}
