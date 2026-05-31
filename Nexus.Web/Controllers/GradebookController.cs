using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Gradebook;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Teacher)]
public class GradebookController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IGradebookService _gradebookService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GradebookController(ApplicationDbContext context, IGradebookService gradebookService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _gradebookService = gradebookService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        var query = _context.Enrollments.AsNoTracking().Include(e => e.Student).Include(e => e.Course).Where(e => e.Course!.TeacherId == teacherId);
        if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId.Value);
        var rows = await query.Select(e => new GradeEntryViewModel
        {
            CourseId = e.CourseId,
            StudentId = e.StudentId,
            CourseName = e.Course!.Name,
            StudentName = e.Student == null ? "Student" : (e.Student.DisplayName ?? e.Student.Email ?? "Student"),
            AssignmentMarks = e.Course.Grades.Where(g => g.StudentId == e.StudentId).Select(g => g.AssignmentMarks).FirstOrDefault(),
            MidtermMarks = e.Course.Grades.Where(g => g.StudentId == e.StudentId).Select(g => g.MidtermMarks).FirstOrDefault(),
            FinalMarks = e.Course.Grades.Where(g => g.StudentId == e.StudentId).Select(g => g.FinalMarks).FirstOrDefault(),
            TotalMarks = e.Course.Grades.Where(g => g.StudentId == e.StudentId).Select(g => g.TotalMarks).FirstOrDefault()
        }).OrderBy(r => r.CourseName).ThenBy(r => r.StudentName).ToListAsync();
        return View(rows);
    }

    public async Task<IActionResult> Edit(string studentId, int courseId)
    {
        if (!await OwnsCourseAsync(courseId)) return Forbid();
        var course = await _context.Courses.AsNoTracking().FirstAsync(c => c.Id == courseId);
        var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == studentId);
        var grade = await _context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.CourseId == courseId && g.StudentId == studentId);
        return View(new GradeEntryViewModel
        {
            CourseId = courseId,
            StudentId = studentId,
            CourseName = course.Name,
            StudentName = student.DisplayName ?? student.Email ?? "Student",
            AssignmentMarks = grade?.AssignmentMarks ?? 0,
            MidtermMarks = grade?.MidtermMarks ?? 0,
            FinalMarks = grade?.FinalMarks ?? 0,
            TotalMarks = grade?.TotalMarks ?? 0
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GradeEntryViewModel model)
    {
        if (!await OwnsCourseAsync(model.CourseId)) return Forbid();
        if (!ModelState.IsValid) return View(model);
        await _gradebookService.UpsertGradeAsync(model.StudentId, model.CourseId, model.AssignmentMarks, model.MidtermMarks, model.FinalMarks);
        TempData["Success"] = "Grade saved.";
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
    }

    private async Task<bool> OwnsCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
    }
}
