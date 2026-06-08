using System.Collections.Generic;

namespace Nexus.Web.ViewModels.Analytics;

public class DashboardViewModel
{
    // Cards
    public long TotalStudents { get; set; }
    public long TotalTeachers { get; set; }
    public long TotalCourses { get; set; }
    public long TotalAssignments { get; set; }
    public long ActiveEnrollments { get; set; }

    // Chart datasets (optional preloaded)
    public IReadOnlyList<LabelValueDto>? MonthlyEnrollments { get; set; }
    public IReadOnlyList<CoursePopularityDto>? CoursePopularity { get; set; }
    public IReadOnlyList<LabelValueDto>? AssignmentSubmissionTrends { get; set; }
    public IReadOnlyList<LabelValueDto>? StudentPerformanceDistribution { get; set; }
}
