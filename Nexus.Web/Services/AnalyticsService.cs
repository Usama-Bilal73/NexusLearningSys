using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Web.ViewModels.Analytics;

namespace Nexus.Web.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalCourses = await _context.Courses.CountAsync(cancellationToken);
        var totalAssignments = await _context.Assignments.CountAsync(cancellationToken);
        var activeEnrollments = await _context.Enrollments.CountAsync(cancellationToken);

        // Roles lookup
        var studentRoleId = await _context.Roles.Where(r => r.Name == ApplicationRoles.Student).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);
        var teacherRoleId = await _context.Roles.Where(r => r.Name == ApplicationRoles.Teacher).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);

        var totalStudents = 0L;
        var totalTeachers = 0L;
        if (!string.IsNullOrEmpty(studentRoleId))
        {
            totalStudents = await _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().Where(ur => ur.RoleId == studentRoleId).LongCountAsync(cancellationToken);
        }

        if (!string.IsNullOrEmpty(teacherRoleId))
        {
            totalTeachers = await _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().Where(ur => ur.RoleId == teacherRoleId).LongCountAsync(cancellationToken);
        }

        var vm = new DashboardViewModel
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TotalCourses = totalCourses,
            TotalAssignments = totalAssignments,
            ActiveEnrollments = activeEnrollments
        };

        return vm;
    }

    public async Task<IReadOnlyList<LabelValueDto>> GetMonthlyEnrollmentsAsync(int months = 12, CancellationToken cancellationToken = default)
    {
        // Enrollment doesn't store date; approximate by using student's CreatedAtUtc where student has an enrollment
        var cutoff = DateTime.UtcNow.AddMonths(-months + 1);

        var query = _context.Enrollments.AsNoTracking()
            .Join(_context.Users.AsNoTracking(), e => e.StudentId, u => u.Id, (e, u) => new { u.CreatedAtUtc })
            .Where(x => x.CreatedAtUtc >= cutoff)
            .GroupBy(x => new { Year = x.CreatedAtUtc.Year, Month = x.CreatedAtUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        var results = await query;

        var list = new List<LabelValueDto>();
        for (int i = months - 1; i >= 0; i--)
        {
            var dt = DateTime.UtcNow.AddMonths(-i);
            var year = dt.Year; var month = dt.Month;
            var found = results.FirstOrDefault(r => r.Year == year && r.Month == month)?.Count ?? 0;
            list.Add(new LabelValueDto(dt.ToString("yyyy-MM"), found));
        }

        return list;
    }

    public async Task<IReadOnlyList<CoursePopularityDto>> GetCoursePopularityAsync(int top = 10, CancellationToken cancellationToken = default)
    {
        var data = await _context.Courses.AsNoTracking()
            .Select(c => new { c.Name, Count = c.Enrollments.Count })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken);

        return data.Select(d => new CoursePopularityDto(d.Name, d.Count)).ToList();
    }

    public async Task<IReadOnlyList<LabelValueDto>> GetAssignmentSubmissionTrendsAsync(int months = 12, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months + 1);
        var data = await _context.Submissions.AsNoTracking()
            .Where(s => s.SubmittedAt >= cutoff)
            .GroupBy(s => new { Year = s.SubmittedAt.Year, Month = s.SubmittedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.LongCount() })
            .ToListAsync(cancellationToken);

        var list = new List<LabelValueDto>();
        for (int i = months - 1; i >= 0; i--)
        {
            var dt = DateTime.UtcNow.AddMonths(-i);
            var year = dt.Year; var month = dt.Month;
            var found = data.FirstOrDefault(r => r.Year == year && r.Month == month)?.Count ?? 0;
            list.Add(new LabelValueDto(dt.ToString("yyyy-MM"), found));
        }

        return list;
    }

    public async Task<IReadOnlyList<LabelValueDto>> GetStudentPerformanceDistributionAsync(CancellationToken cancellationToken = default)
    {
        // Buckets: 0-49,50-64,65-74,75-84,85-100
        var grades = await _context.Grades.AsNoTracking().Select(g => g.TotalMarks).ToListAsync(cancellationToken);
        var buckets = new Dictionary<string, long>
        {
            ["0-49"] = 0,
            ["50-64"] = 0,
            ["65-74"] = 0,
            ["75-84"] = 0,
            ["85-100"] = 0
        };

        foreach (var g in grades)
        {
            if (g < 50) buckets["0-49"]++;
            else if (g < 65) buckets["50-64"]++;
            else if (g < 75) buckets["65-74"]++;
            else if (g < 85) buckets["75-84"]++;
            else buckets["85-100"]++;
        }

        return buckets.Select(kv => new LabelValueDto(kv.Key, kv.Value)).ToList();
    }
}
