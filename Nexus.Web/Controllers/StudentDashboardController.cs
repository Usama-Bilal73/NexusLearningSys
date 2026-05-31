using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Student;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Student)]
public class StudentDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentDashboardController(ApplicationDbContext context, IFileStorageService fileStorage, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileStorage = fileStorage;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = _userManager.GetUserId(User)!;
        var courses = await _context.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId)
            .Include(e => e.Course).ThenInclude(c => c!.Teacher)
            .Select(e => new StudentCourseViewModel
            {
                Id = e.CourseId,
                Name = e.Course!.Name,
                Semester = e.Course.Semester,
                TeacherName = e.Course.Teacher == null ? "Teacher" : (e.Course.Teacher.DisplayName ?? e.Course.Teacher.Email ?? "Teacher")
            }).ToListAsync();
        var assignments = await LoadAssignmentsAsync(studentId);
        return View(new StudentDashboardViewModel { Courses = courses, Assignments = assignments });
    }

    public async Task<IActionResult> Courses()
    {
        var studentId = _userManager.GetUserId(User)!;
        var courses = await _context.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId).Include(e => e.Course).ThenInclude(c => c!.Teacher).ToListAsync();
        return View(courses);
    }

    public async Task<IActionResult> Materials(int courseId)
    {
        if (!await IsEnrolledAsync(courseId)) return Forbid();
        var materials = await _context.CourseMaterials.AsNoTracking().Include(m => m.Course).Where(m => m.CourseId == courseId).OrderByDescending(m => m.UploadedAtUtc).ToListAsync();
        return View(materials);
    }

    public async Task<IActionResult> Assignments()
    {
        var studentId = _userManager.GetUserId(User)!;
        return View(await LoadAssignmentsAsync(studentId));
    }

    public async Task<IActionResult> SubmitAssignment(int id)
    {
        var assignment = await FindEnrolledAssignmentAsync(id);
        if (assignment is null) return NotFound();
        return View(new SubmitAssignmentViewModel { AssignmentId = assignment.Id, AssignmentTitle = assignment.Title });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssignment(SubmitAssignmentViewModel model)
    {
        var assignment = await FindEnrolledAssignmentAsync(model.AssignmentId);
        if (assignment is null) return NotFound();
        if (model.File is null) ModelState.AddModelError(nameof(model.File), "Select a file.");
        if (!ModelState.IsValid)
        {
            model.AssignmentTitle = assignment.Title;
            return View(model);
        }
        try
        {
            var path = await _fileStorage.SaveUploadAsync(model.File!, "assignment-submissions");
            _context.Submissions.Add(new Submission { AssignmentId = model.AssignmentId, StudentId = _userManager.GetUserId(User)!, FilePath = path, SubmittedAt = DateTime.UtcNow, Status = DateTime.UtcNow <= assignment.Deadline ? "Submitted" : "Late" });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Assignment submitted.";
            return RedirectToAction(nameof(AssignmentHistory), new { assignmentId = model.AssignmentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.File), ex.Message);
            model.AssignmentTitle = assignment.Title;
            return View(model);
        }
    }

    public async Task<IActionResult> AssignmentHistory(int assignmentId)
    {
        var assignment = await FindEnrolledAssignmentAsync(assignmentId);
        if (assignment is null) return NotFound();
        var studentId = _userManager.GetUserId(User)!;
        var submissions = await _context.Submissions.AsNoTracking().Where(s => s.StudentId == studentId && s.AssignmentId == assignmentId).OrderByDescending(s => s.SubmittedAt).ToListAsync();
        ViewBag.AssignmentTitle = assignment.Title;
        return View(submissions);
    }

    public async Task<IActionResult> Grades()
    {
        var studentId = _userManager.GetUserId(User)!;
        var grades = await _context.Grades.AsNoTracking().Include(g => g.Course).Where(g => g.StudentId == studentId).OrderBy(g => g.Course!.Name).ToListAsync();
        return View(grades);
    }

    private async Task<IReadOnlyList<StudentAssignmentViewModel>> LoadAssignmentsAsync(string studentId)
    {
        return await _context.Assignments.AsNoTracking().Include(a => a.Course).Where(a => a.Course!.Enrollments.Any(e => e.StudentId == studentId))
            .Select(a => new StudentAssignmentViewModel
            {
                Id = a.Id,
                CourseName = a.Course!.Name,
                Title = a.Title,
                Deadline = a.Deadline,
                Status = a.Submissions.Where(s => s.StudentId == studentId).OrderByDescending(s => s.SubmittedAt).Select(s => s.Status).FirstOrDefault() ?? "Not Submitted"
            }).OrderBy(a => a.Deadline).ToListAsync();
    }

    private async Task<bool> IsEnrolledAsync(int courseId)
    {
        var studentId = _userManager.GetUserId(User)!;
        return await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);
    }

    private async Task<Assignment?> FindEnrolledAssignmentAsync(int assignmentId)
    {
        var studentId = _userManager.GetUserId(User)!;
        return await _context.Assignments.Include(a => a.Course).ThenInclude(c => c!.Enrollments).FirstOrDefaultAsync(a => a.Id == assignmentId && a.Course!.Enrollments.Any(e => e.StudentId == studentId));
    }
}
