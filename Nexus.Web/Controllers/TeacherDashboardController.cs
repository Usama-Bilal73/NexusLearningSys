using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Teacher;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Teacher)]
public class TeacherDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPerformanceAnalyticsService _performanceAnalytics;

    public TeacherDashboardController(ApplicationDbContext context, IFileStorageService fileStorage, UserManager<ApplicationUser> userManager, IPerformanceAnalyticsService performanceAnalytics)
    {
        _context = context;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _performanceAnalytics = performanceAnalytics;
    }

    public async Task<IActionResult> Index()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var courses = await _context.Courses.AsNoTracking()
            .Where(course => course.TeacherId == teacherId)
            .Select(course => new TeacherCourseViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Semester = course.Semester,
                AssignmentCount = course.Assignments.Count,
                MaterialCount = course.Materials.Count,
                StudentCount = course.Enrollments.Count
            })
            .OrderBy(course => course.Name)
            .ToListAsync();
        var alerts = await _performanceAnalytics.GetAtRiskAlertsAsync(teacherId);
        return View(new TeacherDashboardViewModel { Courses = courses, AtRiskAlerts = alerts });
    }

    public async Task<IActionResult> Assignments()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var assignments = await _context.Assignments.AsNoTracking().Include(a => a.Course)
            .Where(a => a.Course != null && a.Course.TeacherId == teacherId)
            .OrderByDescending(a => a.Deadline).ToListAsync();
        return View(assignments);
    }

    public async Task<IActionResult> CreateAssignment() => View(await PopulateCoursesAsync(new AssignmentFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(AssignmentFormViewModel model)
    {
        if (!await OwnsCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select one of your assigned courses.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));
        _context.Assignments.Add(new Assignment { CourseId = model.CourseId, Title = model.Title.Trim(), Description = model.Description.Trim(), Deadline = model.Deadline });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Assignment created.";
        return RedirectToAction(nameof(Assignments));
    }

    public async Task<IActionResult> EditAssignment(int id)
    {
        var assignment = await FindOwnedAssignmentAsync(id);
        if (assignment is null) return NotFound();
        return View(await PopulateCoursesAsync(new AssignmentFormViewModel { Id = assignment.Id, CourseId = assignment.CourseId, Title = assignment.Title, Description = assignment.Description, Deadline = assignment.Deadline }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssignment(int id, AssignmentFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!await OwnsCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select one of your assigned courses.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));
        var assignment = await FindOwnedAssignmentAsync(id);
        if (assignment is null) return NotFound();
        assignment.CourseId = model.CourseId;
        assignment.Title = model.Title.Trim();
        assignment.Description = model.Description.Trim();
        assignment.Deadline = model.Deadline;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Assignment updated.";
        return RedirectToAction(nameof(Assignments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await FindOwnedAssignmentAsync(id);
        if (assignment is null) return NotFound();
        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Assignment deleted.";
        return RedirectToAction(nameof(Assignments));
    }

    public async Task<IActionResult> Materials()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var materials = await _context.CourseMaterials.AsNoTracking().Include(m => m.Course)
            .Where(m => m.Course != null && m.Course.TeacherId == teacherId)
            .OrderByDescending(m => m.UploadedAtUtc).ToListAsync();
        return View(materials);
    }

    public async Task<IActionResult> UploadMaterial() => View(await PopulateCoursesAsync(new MaterialUploadViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMaterial(MaterialUploadViewModel model)
    {
        if (!await OwnsCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select one of your assigned courses.");
        if (model.File is null) ModelState.AddModelError(nameof(model.File), "Select a file.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));
        try
        {
            var path = await _fileStorage.SaveUploadAsync(model.File!, "course-materials");
            _context.CourseMaterials.Add(new CourseMaterial
            {
                CourseId = model.CourseId,
                Title = model.Title.Trim(),
                MaterialType = model.MaterialType,
                Category = model.Category.Trim(),
                OriginalFileName = model.File!.FileName,
                ContentType = model.File.ContentType,
                FilePath = path,
                UploadedByTeacherId = _userManager.GetUserId(User)!
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Material uploaded.";
            return RedirectToAction(nameof(Materials));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.File), ex.Message);
            return View(await PopulateCoursesAsync(model));
        }
    }

    private async Task<Assignment?> FindOwnedAssignmentAsync(int id)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id && a.Course != null && a.Course.TeacherId == teacherId);
    }

    private async Task<bool> OwnsCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Courses.AnyAsync(course => course.Id == courseId && course.TeacherId == teacherId);
    }

    private async Task<T> PopulateCoursesAsync<T>(T model) where T : class
    {
        var teacherId = _userManager.GetUserId(User)!;
        var courses = await _context.Courses.AsNoTracking().Where(course => course.TeacherId == teacherId).OrderBy(course => course.Name).ToListAsync();
        var items = courses.Select(course => new SelectListItem($"{course.Name} ({course.Semester})", course.Id.ToString())).ToList();
        switch (model)
        {
            case AssignmentFormViewModel assignment: assignment.Courses = items; break;
            case MaterialUploadViewModel material: material.Courses = items; break;
        }
        return model;
    }
}
