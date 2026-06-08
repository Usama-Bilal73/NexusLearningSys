using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Data.Models;
using Nexus.Web.ViewModels.Recommendations;

namespace Nexus.Web.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecommendationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RecommendationViewModel>> GetRecommendationsAsync(string userId, int top = 10, CancellationToken cancellationToken = default)
    {
        // Student's enrolled course ids
        var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
        var courseRepo = _unitOfWork.Repository<Course>();
        var gradeRepo = _unitOfWork.Repository<Grade>();

        var enrolledCourseIds = await enrollmentRepo.Query()
            .Where(e => e.StudentId == userId)
            .Select(e => e.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Departments of enrolled courses
        var enrolledDepartmentIds = await courseRepo.Query()
            .Where(c => enrolledCourseIds.Contains(c.Id) && c.DepartmentId != null)
            .Select(c => c.DepartmentId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Candidate courses: not already enrolled
        var candidatesQuery = courseRepo.Query().AsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.Enrollments)
            .Where(c => !enrolledCourseIds.Contains(c.Id));

        var candidates = await candidatesQuery.ToListAsync(cancellationToken);

        if (!candidates.Any()) return Array.Empty<RecommendationViewModel>();

        // Compute course popularity and averages
        var candidateIds = candidates.Select(c => c.Id).ToList();

        var courseEnrollmentCounts = await _unitOfWork.Repository<Course>().Query()
            .Where(c => candidateIds.Contains(c.Id))
            .Select(c => new { c.Id, Count = c.Enrollments.Count })
            .ToDictionaryAsync(x => x.Id, x => (long)x.Count, cancellationToken);

        var courseAverages = await gradeRepo.Query()
            .Where(g => candidateIds.Contains(g.CourseId))
            .GroupBy(g => g.CourseId)
            .Select(g => new { CourseId = g.Key, Avg = g.Average(x => x.TotalMarks) })
            .ToDictionaryAsync(x => x.CourseId, x => (decimal?)x.Avg ?? 0m, cancellationToken);

        // Student overall average
        var studentAverage = await gradeRepo.Query().Where(g => g.StudentId == userId).Select(g => (decimal?)g.TotalMarks).AverageAsync(cancellationToken) ?? 0m;

        // Popularity normalization
        var maxEnroll = courseEnrollmentCounts.Values.DefaultIfEmpty(0).Max();
        var minEnroll = courseEnrollmentCounts.Values.DefaultIfEmpty(0).Min();

        var maxAvg = courseAverages.Values.DefaultIfEmpty(0).Max();
        var minAvg = courseAverages.Values.DefaultIfEmpty(0).Min();

        var results = new List<RecommendationViewModel>();

        foreach (var c in candidates)
        {
            var pop = courseEnrollmentCounts.TryGetValue(c.Id, out var pc) ? pc : 0L;
            var cavg = courseAverages.TryGetValue(c.Id, out var ca) ? ca : 0m;

            // Components
            var deptMatch = (c.DepartmentId != null && enrolledDepartmentIds.Contains(c.DepartmentId.Value)) ? 1m : 0m;

            // Normalize popularity (0-1)
            var popNorm = (maxEnroll == minEnroll) ? 0m : (decimal)(pop - minEnroll) / Math.Max(1, (decimal)(maxEnroll - minEnroll));

            // Normalize course average relative to range
            var avgNorm = (maxAvg == minAvg) ? 0m : (cavg - minAvg) / Math.Max(0.01m, (maxAvg - minAvg));

            // Closeness to student's average (if student's avg is high, prefer courses where class average is good)
            var avgDiff = Math.Abs(studentAverage - cavg);
            var avgCloseness = 1m - Math.Min(1m, avgDiff / 100m);

            // Weighted score: deptMatch (0.3), popularity (0.3), avgNorm (0.25), avgCloseness (0.15)
            var score = (deptMatch * 0.30m) + (popNorm * 0.30m) + (avgNorm * 0.25m) + (avgCloseness * 0.15m);

            var reasons = new List<string>();
            if (deptMatch > 0) reasons.Add("Related to your current department(s)");
            if (pop > 0) reasons.Add($"Popular: {pop} students enrolled");
            if (cavg > 0) reasons.Add($"Average grade: {cavg:0.##}%");

            results.Add(new RecommendationViewModel
            {
                CourseId = c.Id,
                CourseName = c.Name,
                DepartmentName = c.Department?.Name,
                EnrollmentCount = pop,
                CourseAverage = Math.Round(cavg, 2),
                Score = Math.Round(score * 100m, 2), // percentage-style score
                Reason = reasons.Any() ? string.Join("; ", reasons) : "Based on popularity and course performance"
            });
        }

        return results.OrderByDescending(r => r.Score).Take(top).ToList();
    }
}
