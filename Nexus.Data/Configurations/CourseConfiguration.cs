using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Identity;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(course => course.Id);
        builder.Property(course => course.Name).HasMaxLength(160).IsRequired();
        builder.Property(course => course.Semester).HasMaxLength(60).IsRequired();
        builder.Property(course => course.TeacherId).HasMaxLength(450).IsRequired();
        builder.HasIndex(course => new { course.Name, course.Semester }).IsUnique();

        builder.HasOne(course => course.Department)
            .WithMany(department => department.Courses)
            .HasForeignKey(course => course.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(course => course.Teacher)
            .WithMany()
            .HasForeignKey(course => course.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
