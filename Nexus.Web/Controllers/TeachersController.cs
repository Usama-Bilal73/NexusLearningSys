using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Web.ViewModels.Admin;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class TeachersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TeachersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Teacher);
        var query = usersInRole.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(user => (user.Email ?? string.Empty).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || user.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || user.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var total = query.Count();
        var users = query.OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Skip((Math.Max(pageNumber, 1) - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserListItemViewModel
            {
                Id = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? string.Empty : user.FullName,
                Email = user.Email ?? string.Empty,
                Role = ApplicationRoles.Teacher,
                CreatedAtUtc = user.CreatedAtUtc
            })
            .ToList();

        return View(new PagedResult<AdminUserListItemViewModel>
        {
            Items = users,
            SearchTerm = searchTerm,
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = pageSize,
            TotalItems = total
        });
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (user is null || !await IsInManagedRoleAsync(user)) return NotFound();
        return View(ToFormModel(user));
    }

    public IActionResult Create() => View(new AdminUserFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required for new accounts.");
        }

        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            DisplayName = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, ApplicationRoles.Teacher);
            TempData["Success"] = "Teacher account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        AddIdentityErrors(result);
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await IsInManagedRoleAsync(user)) return NotFound();
        return View(ToFormModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, AdminUserFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await IsInManagedRoleAsync(user)) return NotFound();

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.DisplayName = $"{user.FirstName} {user.LastName}";
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
            {
                AddIdentityErrors(passwordResult);
                return View(model);
            }
        }

        TempData["Success"] = "Teacher account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await IsInManagedRoleAsync(user)) return NotFound();
        return View(ToFormModel(user));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await IsInManagedRoleAsync(user)) return NotFound();
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(ToFormModel(user));
        }

        TempData["Success"] = "Teacher account deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsInManagedRoleAsync(ApplicationUser user) => await _userManager.IsInRoleAsync(user, ApplicationRoles.Teacher);

    private static AdminUserFormViewModel ToFormModel(ApplicationUser user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email ?? string.Empty
    };

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
