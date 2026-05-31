using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
{
    public void Configure(EntityTypeBuilder<CourseMaterial> builder)
    {
        builder.ToTable("CourseMaterials");
        builder.HasKey(material => material.Id);
        builder.Property(material => material.Title).HasMaxLength(180).IsRequired();
        builder.Property(material => material.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(material => material.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(material => material.ContentType).HasMaxLength(120);
        builder.Property(material => material.UploadedByTeacherId).HasMaxLength(450).IsRequired();
        builder.HasIndex(material => new { material.CourseId, material.MaterialType });

        builder.HasOne(material => material.Course)
            .WithMany(course => course.Materials)
            .HasForeignKey(material => material.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(material => material.UploadedByTeacher)
            .WithMany()
            .HasForeignKey(material => material.UploadedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
