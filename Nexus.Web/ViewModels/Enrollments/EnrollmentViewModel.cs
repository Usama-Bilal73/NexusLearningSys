namespace Nexus.Web.ViewModels.Enrollments;

public class EnrollmentViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateTime EnrolledAtUtc { get; set; }
}
