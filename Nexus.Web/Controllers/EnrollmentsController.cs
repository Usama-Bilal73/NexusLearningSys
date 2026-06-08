using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Web.Services;
using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Web.ViewModels.Enrollments;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class EnrollmentsController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentsController(IEnrollmentService enrollmentService, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        _enrollmentService = enrollmentService;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? search, int? courseId, string? studentId)
    {
        var items = await _enrollmentService.GetEnrollmentsAsync(search, courseId, studentId);
        ViewBag.Courses = (await _unitOfWork.Repository<Course>().Query().AsNoTracking().OrderBy(c => c.Name).ToListAsync()).Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()));
        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Courses = (await _unitOfWork.Repository<Course>().Query().AsNoTracking().OrderBy(c => c.Name).ToListAsync()).Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()));
        ViewBag.Students = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student)).Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(u.DisplayName ?? u.Email ?? u.UserName, u.Id));
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string studentId, int courseId)
    {
        if (string.IsNullOrWhiteSpace(studentId)) ModelState.AddModelError("studentId", "Select a student");
        if (!ModelState.IsValid) return await Create();
        var ok = await _enrollmentService.EnrollStudentAsync(studentId, courseId);
        TempData[ok ? "Success" : "Error"] = ok ? "Student enrolled." : "Enrollment failed or student already enrolled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(string studentId, int courseId)
    {
        if (string.IsNullOrWhiteSpace(studentId)) return BadRequest();
        var ok = await _enrollmentService.WithdrawStudentAsync(studentId, courseId);
        TempData[ok ? "Success" : "Error"] = ok ? "Student withdrawn." : "Withdraw failed.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> BulkImport()
    {
        ViewBag.Courses = (await _unitOfWork.Repository<Course>().Query().AsNoTracking().OrderBy(c => c.Name).ToListAsync()).Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()));
        return View(new BulkEnrollmentViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkImport(BulkEnrollmentViewModel model)
    {
        if (model.File is null || model.File.Length == 0) { ModelState.AddModelError(nameof(model.File), "Select a CSV file."); return await BulkImport(); }
        if (model.CourseId == 0) { ModelState.AddModelError(nameof(model.CourseId), "Select a course."); return await BulkImport(); }

        var users = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student);
        var emailToUser = users.ToDictionary(u => (u.Email ?? string.Empty).Trim().ToLowerInvariant(), u => u.Id);

        using var stream = model.File.OpenReadStream();
        using var reader = new System.IO.StreamReader(stream);
        int processed = 0, enrolled = 0, failed = 0;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var email = line.Trim().Trim('"').ToLowerInvariant();
            processed++;
            if (!emailToUser.TryGetValue(email, out var userId)) { failed++; continue; }
            var ok = await _enrollmentService.EnrollStudentAsync(userId, model.CourseId);
            if (ok) enrolled++; else failed++;
        }

        TempData["Success"] = $"Processed {processed} rows. Enrolled: {enrolled}. Failed: {failed}.";
        return RedirectToAction(nameof(Index));
    }
}
