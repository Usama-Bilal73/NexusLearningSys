using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(question => question.Id);
        builder.Property(question => question.Text).HasMaxLength(1000).IsRequired();
        builder.Property(question => question.OptionA).HasMaxLength(400).IsRequired();
        builder.Property(question => question.OptionB).HasMaxLength(400).IsRequired();
        builder.Property(question => question.OptionC).HasMaxLength(400).IsRequired();
        builder.Property(question => question.OptionD).HasMaxLength(400).IsRequired();
        builder.Property(question => question.CorrectOption).HasMaxLength(1).IsRequired();
        builder.Property(question => question.Points).HasPrecision(5, 2);

        builder.HasOne(question => question.Quiz)
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(question => question.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
