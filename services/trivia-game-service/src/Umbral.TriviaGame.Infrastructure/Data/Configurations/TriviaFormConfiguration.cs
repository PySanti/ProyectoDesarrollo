using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Configurations;

internal sealed class TriviaFormConfiguration : IEntityTypeConfiguration<TriviaForm>
{
    public void Configure(EntityTypeBuilder<TriviaForm> builder)
    {
        builder.ToTable("TriviaForms");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasConversion(ValueConverters.TriviaFormIdConverter);

        builder.Property(f => f.Title)
            .HasConversion(ValueConverters.FormTitleConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.CreatedByOperatorId)
            .HasConversion(ValueConverters.OperatorIdConverter)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(f => f.CreatedAtUtc)
            .IsRequired();

        builder.Property(f => f.UpdatedAtUtc)
            .IsRequired();

        builder.Ignore(f => f.IsComplete);
        builder.Ignore(f => f.DomainEvents);

        builder.HasMany(f => f.Questions)
            .WithOne()
            .HasForeignKey("TriviaFormId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(f => f.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
