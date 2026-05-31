using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Data.Models;

namespace Nexus.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(department => department.Name).IsUnique();
    }
}
