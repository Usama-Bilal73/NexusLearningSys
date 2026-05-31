using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");
        builder.HasKey(grade => new { grade.StudentId, grade.CourseId });
        builder.Property(grade => grade.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(grade => grade.AssignmentMarks).HasPrecision(5, 2);
        builder.Property(grade => grade.MidtermMarks).HasPrecision(5, 2);
        builder.Property(grade => grade.FinalMarks).HasPrecision(5, 2);
        builder.Property(grade => grade.TotalMarks).HasPrecision(5, 2);

        builder.HasOne(grade => grade.Student)
            .WithMany()
            .HasForeignKey(grade => grade.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(grade => grade.Course)
            .WithMany(course => course.Grades)
            .HasForeignKey(grade => grade.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
