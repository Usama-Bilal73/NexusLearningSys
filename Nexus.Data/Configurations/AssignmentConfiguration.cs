using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Title).HasMaxLength(180).IsRequired();
        builder.Property(assignment => assignment.Description).HasMaxLength(2000).IsRequired();
        builder.Property(assignment => assignment.Deadline).IsRequired();
        builder.HasIndex(assignment => new { assignment.CourseId, assignment.Deadline });

        builder.HasOne(assignment => assignment.Course)
            .WithMany(course => course.Assignments)
            .HasForeignKey(assignment => assignment.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
