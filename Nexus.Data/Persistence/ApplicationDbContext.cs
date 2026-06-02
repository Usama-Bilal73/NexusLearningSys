using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;

namespace Nexus.Data.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<Grade> Grades => Set<Grade>();

    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<Answer> Answers => Set<Answer>();

    // Abu Bakar modules
    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    public DbSet<GradeWeight> GradeWeights => Set<GradeWeight>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("nexus");
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<Attendance>(entity =>
        {
            entity.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.MarkedByTeacher)
                .WithMany()
                .HasForeignKey(a => a.MarkedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
