using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexus.Web.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override Task OnConnectedAsync()
    {
        // Clients are authenticated; default UserIdentifier is ClaimTypes.NameIdentifier
        return base.OnConnectedAsync();
    }
}
