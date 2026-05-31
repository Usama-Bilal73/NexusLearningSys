namespace Nexus.Web.ViewModels.Analytics;

public class StudentPerformanceAlertViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal AssignmentAverage { get; set; }
    public decimal QuizAverage { get; set; }
    public int SubmissionCount { get; set; }
    public int AssignmentCount { get; set; }
    public string Status { get; set; } = "At Risk";
    public string Reason { get; set; } = string.Empty;
}
