using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Repository;

namespace Nexus.Web.Controllers;

[Authorize]
public class CourseAssistantController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICourseRagService _ragService;
    private readonly IOpenAiLearningService _openAiLearningService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CourseAssistantController(ApplicationDbContext context, ICourseRagService ragService, IOpenAiLearningService openAiLearningService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _ragService = ragService;
        _openAiLearningService = openAiLearningService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index() => View(await PopulateCoursesAsync(new CourseAssistantChatViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CourseAssistantChatViewModel model, CancellationToken cancellationToken)
    {
        if (!await CanAccessCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select a course you can access.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));
        var (chunks, sources) = await _ragService.RetrieveAsync(model.CourseId, model.Question, cancellationToken);
        model.Sources = sources;
        model.Answer = chunks.Count == 0
            ? "No syllabus or lecture-note text has been indexed for this course yet. Upload PDF syllabus or lecture notes first."
            : await _openAiLearningService.AnswerCourseQuestionAsync(model.Question, chunks, cancellationToken);
        return View(await PopulateCoursesAsync(model));
    }

    private async Task<bool> CanAccessCourseAsync(int courseId)
    {
        var userId = _userManager.GetUserId(User)!;
        if (User.IsInRole(ApplicationRoles.Teacher)) return await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == userId);
        if (User.IsInRole(ApplicationRoles.Student)) return await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == userId);
        return User.IsInRole(ApplicationRoles.Admin);
    }

    private async Task<CourseAssistantChatViewModel> PopulateCoursesAsync(CourseAssistantChatViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var courses = _context.Courses.AsNoTracking().AsQueryable();
        if (User.IsInRole(ApplicationRoles.Teacher)) courses = courses.Where(c => c.TeacherId == userId);
        if (User.IsInRole(ApplicationRoles.Student)) courses = courses.Where(c => c.Enrollments.Any(e => e.StudentId == userId));
        model.Courses = await courses.OrderBy(c => c.Name).Select(c => new SelectListItem($"{c.Name} ({c.Semester})", c.Id.ToString())).ToListAsync();
        return model;
    }
}
