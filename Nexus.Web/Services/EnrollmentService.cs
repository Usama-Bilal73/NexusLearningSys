using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Data.Models;
using Nexus.Web.ViewModels.Enrollments;

namespace Nexus.Web.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly IAnalyticsService _analytics;

    public EnrollmentService(IUnitOfWork unitOfWork, INotificationService notifications, ILogger<EnrollmentService> logger, IAnalyticsService analytics)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _logger = logger;
        _analytics = analytics;
    }

    public async Task<bool> EnrollStudentAsync(string studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Enrollment>();
        // Prevent duplicate
        var exists = await repo.Query().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
        if (exists) return false;

        // Capacity check if Course has a Capacity property
        var courseRepo = _unitOfWork.Repository<Course>();
        var course = await courseRepo.Query().Include(c => c.Enrollments).FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course is null) return false;

        var capacityProp = course.GetType().GetProperty("Capacity");
        if (capacityProp != null)
        {
            var val = capacityProp.GetValue(course);
            if (val is int cap && cap > 0)
            {
                if (course.Enrollments.Count >= cap)
                {
                    return false;
                }
            }
        }

        var enrollment = new Enrollment { StudentId = studentId, CourseId = courseId };
        await repo.AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Audit: log
        _logger.LogInformation("Enrolled student {StudentId} into course {CourseId} at {Time}", studentId, courseId, DateTime.UtcNow);

        // Notification
        try
        {
            await _notifications.CreateSystemAnnouncementAsync($"Enrollment: {course.Name}", $"You have been enrolled into {course.Name}.", studentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send enrollment notification to {StudentId}", studentId);
        }

        // Update analytics (warm)
        try { await _analytics.GetDashboardAsync(cancellationToken); } catch { }

        return true;
    }

    public async Task<bool> WithdrawStudentAsync(string studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Enrollment>();
        var enrollment = await repo.Query().FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
        if (enrollment is null) return false;
        repo.Remove(enrollment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Withdrawn student {StudentId} from course {CourseId} at {Time}", studentId, courseId, DateTime.UtcNow);

        // Notification
        try
        {
            await _notifications.CreateSystemAnnouncementAsync($"Withdrawal: {courseId}", $"You have been withdrawn from course ({courseId}).", studentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send withdrawal notification to {StudentId}", studentId);
        }

        try { await _analytics.GetDashboardAsync(cancellationToken); } catch { }

        return true;
    }

    public async Task<IReadOnlyList<EnrollmentViewModel>> GetEnrollmentsAsync(string? search = null, int? courseId = null, string? studentId = null, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Enrollment>();
        var query = repo.Query().AsNoTracking().Include(e => e.Course).Include(e => e.Student).AsQueryable();
        if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId.Value);
        if (!string.IsNullOrWhiteSpace(studentId)) query = query.Where(e => e.StudentId == studentId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            query = query.Where(e => e.Course!.Name.ToLower().Contains(s) || (e.Student != null && (e.Student.DisplayName != null && e.Student.DisplayName.ToLower().Contains(s)) || (e.Student.Email != null && e.Student.Email.ToLower().Contains(s))));
        }

        var list = await query.OrderBy(e => e.Course!.Name).ThenBy(e => e.Student!.DisplayName).ToListAsync(cancellationToken);
        return list.Select(e => new EnrollmentViewModel
        {
            CourseId = e.CourseId,
            CourseName = e.Course?.Name ?? string.Empty,
            StudentId = e.StudentId,
            StudentName = e.Student?.DisplayName ?? e.Student?.Email ?? string.Empty,
            EnrolledAtUtc = DateTime.UtcNow // no persisted timestamp; using now for view
        }).ToList();
    }
}
