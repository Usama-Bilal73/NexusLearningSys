using Nexus.Web.ViewModels.Analytics;

namespace Nexus.Web.Services;

public interface IAnalyticsService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabelValueDto>> GetMonthlyEnrollmentsAsync(int months = 12, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoursePopularityDto>> GetCoursePopularityAsync(int top = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabelValueDto>> GetAssignmentSubmissionTrendsAsync(int months = 12, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabelValueDto>> GetStudentPerformanceDistributionAsync(CancellationToken cancellationToken = default);
}
