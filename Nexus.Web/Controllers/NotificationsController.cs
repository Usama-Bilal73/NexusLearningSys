using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Web.Services;

namespace Nexus.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(INotificationService notifications, UserManager<ApplicationUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var items = await _notifications.GetNotificationsAsync(userId, 1, 100);
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var item = await _notifications.GetNotificationAsync(id, userId);
        if (item is null) return NotFound();
        await _notifications.MarkAsReadAsync(id, userId);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _notifications.MarkAsReadAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _userManager.GetUserId(User)!;
        await _notifications.MarkAllAsReadAsync(userId);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSystemAnnouncement(string title, string message, string recipientUserId)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(recipientUserId)) return BadRequest();
        await _notifications.CreateSystemAnnouncementAsync(title, message, recipientUserId);
        return RedirectToAction(nameof(Index));
    }
}
