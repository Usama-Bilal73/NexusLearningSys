using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Nexus.Web.ViewModels.Student;

public class StudentDashboardViewModel
{
    public IReadOnlyList<StudentCourseViewModel> Courses { get; set; } = [];
    public IReadOnlyList<StudentAssignmentViewModel> Assignments { get; set; } = [];
}

public class StudentCourseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
}

public class StudentAssignmentViewModel
{
    public int Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "Not Submitted";
}

public class SubmitAssignmentViewModel
{
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    [Required] public IFormFile? File { get; set; }
}
