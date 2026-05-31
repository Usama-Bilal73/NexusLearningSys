using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Semester { get; set; } = string.Empty;

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public ApplicationUser? Teacher { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
