using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Enrollment
{
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }
}
