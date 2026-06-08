using Microsoft.AspNetCore.SignalR;
using Nexus.Data.Models;
using Nexus.Web.Hubs;

namespace Nexus.Web.Services;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public RealTimeNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null) return;

        var payload = new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            notificationType = notification.NotificationType.ToString(),
            isRead = notification.IsRead,
            createdAtUtc = notification.CreatedAtUtc
        };

        // Send to the specific user by user identifier (assumes user id is the NameIdentifier)
        await _hubContext.Clients.User(notification.RecipientUserId).SendAsync("ReceiveNotification", payload, cancellationToken);
    }
}
