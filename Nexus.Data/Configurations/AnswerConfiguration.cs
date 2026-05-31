using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("Answers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.SelectedOption).HasMaxLength(1);
        builder.Property(answer => answer.PointsEarned).HasPrecision(5, 2);
        builder.HasIndex(answer => new { answer.QuizAttemptId, answer.QuestionId }).IsUnique();

        builder.HasOne(answer => answer.QuizAttempt)
            .WithMany(attempt => attempt.Answers)
            .HasForeignKey(answer => answer.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(answer => answer.Question)
            .WithMany()
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
