using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable("Quizzes");
        builder.HasKey(quiz => quiz.Id);
        builder.Property(quiz => quiz.Title).HasMaxLength(180).IsRequired();
        builder.Property(quiz => quiz.Description).HasMaxLength(1000);
        builder.HasIndex(quiz => new { quiz.CourseId, quiz.IsPublished });

        builder.HasOne(quiz => quiz.Course)
            .WithMany(course => course.Quizzes)
            .HasForeignKey(quiz => quiz.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
