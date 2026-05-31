using System.ComponentModel.DataAnnotations;

namespace Nexus.Web.ViewModels.Gradebook;

public class GradeEntryViewModel
{
    [Required] public int CourseId { get; set; }
    [Required] public string StudentId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    [Range(0, 100)] public decimal AssignmentMarks { get; set; }
    [Range(0, 100)] public decimal MidtermMarks { get; set; }
    [Range(0, 100)] public decimal FinalMarks { get; set; }
    public decimal TotalMarks { get; set; }
}
