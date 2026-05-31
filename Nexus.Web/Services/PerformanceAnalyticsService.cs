using Microsoft.EntityFrameworkCore;
using Nexus.Data.Persistence;
using Nexus.Web.ViewModels.Analytics;

namespace Nexus.Web.Services;

public interface IPerformanceAnalyticsService
{
    Task<IReadOnlyList<StudentPerformanceAlertViewModel>> GetAtRiskAlertsAsync(string teacherId, CancellationToken cancellationToken = default);
}

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private const decimal MinimumAssignmentAverage = 50m;
    private const decimal MinimumQuizAverage = 50m;
    private readonly ApplicationDbContext _context;

    public PerformanceAnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StudentPerformanceAlertViewModel>> GetAtRiskAlertsAsync(string teacherId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _context.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)!.ThenInclude(c => c!.Assignments)
            .Where(e => e.Course!.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

        var alerts = new List<StudentPerformanceAlertViewModel>();
        foreach (var enrollment in enrollments)
        {
            var assignmentIds = enrollment.Course!.Assignments.Select(a => a.Id).ToList();
            var submissions = await _context.Submissions.AsNoTracking().Where(s => s.StudentId == enrollment.StudentId && assignmentIds.Contains(s.AssignmentId)).ToListAsync(cancellationToken);
            var assignmentGrade = await _context.Grades.AsNoTracking().Where(g => g.StudentId == enrollment.StudentId && g.CourseId == enrollment.CourseId).Select(g => (decimal?)g.AssignmentMarks).FirstOrDefaultAsync(cancellationToken) ?? 0;
            var quizAttempts = await _context.QuizAttempts.AsNoTracking().Where(a => a.StudentId == enrollment.StudentId && a.Quiz!.CourseId == enrollment.CourseId && a.SubmittedAtUtc != null).ToListAsync(cancellationToken);
            var quizAverage = quizAttempts.Count == 0 ? 0 : quizAttempts.Average(a => a.Score);
            var expectedSubmissions = assignmentIds.Count;
            var missingSubmissions = Math.Max(0, expectedSubmissions - submissions.Select(s => s.AssignmentId).Distinct().Count());

            var reasons = new List<string>();
            if (assignmentGrade < MinimumAssignmentAverage) reasons.Add($"assignment average {assignmentGrade:0.##}%");
            if (quizAverage < MinimumQuizAverage) reasons.Add($"quiz average {quizAverage:0.##}");
            if (expectedSubmissions > 0 && missingSubmissions > 0) reasons.Add($"{missingSubmissions} missing submission(s)");

            if (reasons.Count > 0)
            {
                alerts.Add(new StudentPerformanceAlertViewModel
                {
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student?.DisplayName ?? enrollment.Student?.Email ?? "Student",
                    CourseName = enrollment.Course.Name,
                    AssignmentAverage = assignmentGrade,
                    QuizAverage = Math.Round(quizAverage, 2),
                    SubmissionCount = submissions.Count,
                    AssignmentCount = expectedSubmissions,
                    Reason = string.Join(", ", reasons)
                });
            }
        }

        return alerts.OrderBy(a => a.CourseName).ThenBy(a => a.StudentName).ToList();
    }
}
