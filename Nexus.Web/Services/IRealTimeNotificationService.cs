using Nexus.Data.Models;

namespace Nexus.Web.Services;

public interface IRealTimeNotificationService
{
    Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
}
