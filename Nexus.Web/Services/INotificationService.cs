using Nexus.Data.Models;

namespace Nexus.Web.Services;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<Notification?> GetNotificationAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    Task CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    // Convenience helpers
    Task CreateSystemAnnouncementAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default);
    Task CreateCourseAnnouncementAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default);
    Task CreateAssignmentDueNotificationAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default);
    Task CreateQuizReminderAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default);
}
