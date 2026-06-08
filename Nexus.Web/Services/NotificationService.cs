using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Data.Models;

namespace Nexus.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealTimeNotificationService? _realTime;

    public NotificationService(IUnitOfWork unitOfWork, IRealTimeNotificationService? realTime = null)
    {
        _unitOfWork = unitOfWork;
        _realTime = realTime;
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Notification>().Query().Where(n => n.RecipientUserId == userId && !n.IsRead).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetNotificationAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<Notification>().Query().FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, cancellationToken);
    }

    public async Task MarkAsReadAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notification = await repo.Query().FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, cancellationToken);
        if (notification is null) return;
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            repo.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var items = await repo.Query().Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var n in items) n.IsRead = true;
        if (items.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Push real-time notification if available
        try
        {
            if (_realTime is not null)
            {
                await _realTime.SendNotificationAsync(notification, cancellationToken);
            }
        }
        catch
        {
            // Swallow real-time failures to avoid breaking primary flow
        }
    }

    public Task CreateSystemAnnouncementAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default)
        => CreateNotificationAsync(new Notification { RecipientUserId = recipientUserId, Title = title, Message = message, NotificationType = NotificationType.System });

    public Task CreateCourseAnnouncementAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default)
        => CreateNotificationAsync(new Notification { RecipientUserId = recipientUserId, Title = title, Message = message, NotificationType = NotificationType.CourseAnnouncement });

    public Task CreateAssignmentDueNotificationAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default)
        => CreateNotificationAsync(new Notification { RecipientUserId = recipientUserId, Title = title, Message = message, NotificationType = NotificationType.AssignmentDue });

    public Task CreateQuizReminderAsync(string title, string message, string recipientUserId, CancellationToken cancellationToken = default)
        => CreateNotificationAsync(new Notification { RecipientUserId = recipientUserId, Title = title, Message = message, NotificationType = NotificationType.QuizReminder });
}
