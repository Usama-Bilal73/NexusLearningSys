using Nexus.Web.ViewModels.Enrollments;

namespace Nexus.Web.Services;

public interface IEnrollmentService
{
    Task<bool> EnrollStudentAsync(string studentId, int courseId, CancellationToken cancellationToken = default);
    Task<bool> WithdrawStudentAsync(string studentId, int courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentViewModel>> GetEnrollmentsAsync(string? search = null, int? courseId = null, string? studentId = null, CancellationToken cancellationToken = default);
}
