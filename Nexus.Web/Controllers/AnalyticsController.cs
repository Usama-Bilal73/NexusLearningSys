using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Data.Identity;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Analytics;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _analytics.GetDashboardAsync();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyEnrollments(int months = 12)
    {
        var data = await _analytics.GetMonthlyEnrollmentsAsync(months);
        return Ok(data);
    }

    [HttpGet]
    public async Task<IActionResult> CoursePopularity(int top = 10)
    {
        var data = await _analytics.GetCoursePopularityAsync(top);
        return Ok(data);
    }

    [HttpGet]
    public async Task<IActionResult> AssignmentSubmissionTrends(int months = 12)
    {
        var data = await _analytics.GetAssignmentSubmissionTrendsAsync(months);
        return Ok(data);
    }

    [HttpGet]
    public async Task<IActionResult> StudentPerformanceDistribution()
    {
        var data = await _analytics.GetStudentPerformanceDistributionAsync();
        return Ok(data);
    }
}
