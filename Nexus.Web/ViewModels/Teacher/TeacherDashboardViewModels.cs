using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nexus.Data.Models;
using Nexus.Web.ViewModels.Analytics;

namespace Nexus.Web.ViewModels.Teacher;

public class TeacherDashboardViewModel
{
    public IReadOnlyList<TeacherCourseViewModel> Courses { get; set; } = [];
    public IReadOnlyList<StudentPerformanceAlertViewModel> AtRiskAlerts { get; set; } = [];
}

public class TeacherCourseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int AssignmentCount { get; set; }
    public int MaterialCount { get; set; }
    public int StudentCount { get; set; }
}

public class AssignmentFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required] public DateTime Deadline { get; set; } = DateTime.UtcNow.AddDays(7);
    [Required] public int CourseId { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}

public class MaterialUploadViewModel
{
    [Required] public int CourseId { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [Required] public CourseMaterialType MaterialType { get; set; }
    [Required, StringLength(80)] public string Category { get; set; } = "General";
    [Required] public IFormFile? File { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}
