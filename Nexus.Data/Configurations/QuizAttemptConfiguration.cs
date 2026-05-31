using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("QuizAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(attempt => attempt.Score).HasPrecision(6, 2);
        builder.HasIndex(attempt => new { attempt.QuizId, attempt.StudentId });

        builder.HasOne(attempt => attempt.Quiz)
            .WithMany(quiz => quiz.Attempts)
            .HasForeignKey(attempt => attempt.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(attempt => attempt.Student)
            .WithMany()
            .HasForeignKey(attempt => attempt.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
