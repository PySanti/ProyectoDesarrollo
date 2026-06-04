using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Configurations;

internal sealed class TriviaInscripcionConfiguration : IEntityTypeConfiguration<TriviaInscripcion>
{
    public void Configure(EntityTypeBuilder<TriviaInscripcion> builder)
    {
        builder.ToTable("trivia_inscripciones");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(ValueConverters.TriviaInscripcionIdConverter);

        builder.Property(i => i.PartidaId)
            .HasConversion(ValueConverters.PartidaIdConverter)
            .IsRequired();

        builder.Property(i => i.UsuarioId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.EquipoId)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(i => i.FechaInscripcion)
            .IsRequired();

        builder.HasIndex(i => new { i.PartidaId, i.UsuarioId }).IsUnique();
    }
}
