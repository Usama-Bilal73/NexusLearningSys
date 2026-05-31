using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Web.ViewModels.Admin;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class RoleManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleManagementController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(user => user.Email).ToListAsync();
        var model = new RoleManagementViewModel
        {
            Users = await Task.WhenAll(users.Select(async user => new UserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? string.Empty : user.FullName,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty
            }))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        if (!ApplicationRoles.All.Contains(role))
        {
            TempData["Error"] = "Select a valid role.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["Error"] = "User was not found.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            TempData["Error"] = string.Join(" ", removeResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        TempData[addResult.Succeeded ? "Success" : "Error"] = addResult.Succeeded
            ? $"Updated {user.Email}'s role to {role}."
            : string.Join(" ", addResult.Errors.Select(error => error.Description));

        return RedirectToAction(nameof(Index));
    }
}
