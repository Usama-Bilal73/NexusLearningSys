using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(submission => new { submission.StudentId, submission.AssignmentId });
        builder.Property(submission => submission.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(submission => submission.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(submission => submission.SubmittedAt).IsRequired();

        builder.HasOne(submission => submission.Student)
            .WithMany()
            .HasForeignKey(submission => submission.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(submission => submission.Assignment)
            .WithMany(assignment => assignment.Submissions)
            .HasForeignKey(submission => submission.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
