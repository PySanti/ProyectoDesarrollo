using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Infrastructure.Data.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id)
            .HasConversion(ValueConverters.QuestionIdConverter);

        builder.Property(q => q.Text)
            .HasConversion(ValueConverters.QuestionTextConverter)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(q => q.AssignedScore)
            .HasConversion(ValueConverters.AssignedScoreConverter)
            .IsRequired();

        builder.Property(q => q.TimeLimit)
            .HasConversion(ValueConverters.TimeLimitConverter)
            .HasColumnName("TimeLimitSeconds")
            .IsRequired();

        builder.Property(q => q.DisplayOrder)
            .IsRequired();

        builder.OwnsMany(q => q.Options, opt =>
        {
            opt.WithOwner().HasForeignKey("QuestionId");
            opt.ToTable("QuestionOptions");

            opt.Property(o => o.Text)
                .HasConversion(ValueConverters.OptionTextConverter)
                .HasMaxLength(500)
                .IsRequired();

            opt.Property(o => o.IsCorrect)
                .IsRequired();

            opt.Property(o => o.Orden)
                .IsRequired();
        });

        builder.Navigation(q => q.Options)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
