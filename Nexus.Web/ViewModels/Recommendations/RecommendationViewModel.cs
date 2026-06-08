namespace Nexus.Web.ViewModels.Recommendations;

public class RecommendationViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public decimal Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long EnrollmentCount { get; set; }
    public decimal CourseAverage { get; set; }
}
