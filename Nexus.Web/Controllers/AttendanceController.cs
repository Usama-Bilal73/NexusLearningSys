using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;

namespace Nexus.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacherId = _userManager.GetUserId(User)!;
            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.TeacherId == teacherId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(courses);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            return RedirectToAction(nameof(MyAttendance));
        }
        else if (User.IsInRole(ApplicationRoles.Admin))
        {
            var courses = await _context.Courses.AsNoTracking()
                .Include(c => c.Teacher)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View("AdminIndex", courses);
        }

        return Forbid();
    }

    // ─── TEACHER: Mark Attendance ────────────────────────────────────────────

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Mark(int courseId, DateTime? date)
    {
        if (!await OwnsCourseAsync(courseId)) return Forbid();

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null) return NotFound();

        var markDate = date?.Date ?? DateTime.Today;

        var students = await _context.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .Select(e => e.Student!)
            .ToListAsync();

        var existing = await _context.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.Date.Date == markDate)
            .ToDictionaryAsync(a => a.StudentId, a => a.Status);

        ViewBag.CourseName = course.Name;
        ViewBag.CourseId   = courseId;
        ViewBag.Date       = markDate;
        ViewBag.Existing   = existing;
        ViewBag.Statuses   = AttendanceStatus.All;
        return View(students);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> MarkSave(int courseId, DateTime date, Dictionary<string, string> statuses)
    {
        if (!await OwnsCourseAsync(courseId)) return Forbid();

        var teacherId = _userManager.GetUserId(User)!;
        var markDate  = date.Date;

        // Remove existing records for this date/course and re-insert
        var existing = await _context.Attendances
            .Where(a => a.CourseId == courseId && a.Date.Date == markDate)
            .ToListAsync();
        _context.Attendances.RemoveRange(existing);

        foreach (var (studentId, status) in statuses)
        {
            if (!AttendanceStatus.All.Contains(status)) continue;
            _context.Attendances.Add(new Attendance
            {
                CourseId          = courseId,
                StudentId         = studentId,
                Date              = markDate,
                Status            = status,
                MarkedByTeacherId = teacherId,
                MarkedAtUtc       = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Attendance saved for {markDate:dd MMM yyyy}.";
        return RedirectToAction(nameof(Report), new { courseId });
    }

    // ─── TEACHER: Attendance Report ──────────────────────────────────────────

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    public async Task<IActionResult> Report(int courseId)
    {
        if (User.IsInRole(ApplicationRoles.Teacher) && !await OwnsCourseAsync(courseId)) return Forbid();

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null) return NotFound();

        var records = await _context.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Include(a => a.Student)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var students = await _context.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .Select(e => e.Student!)
            .OrderBy(s => s.DisplayName)
            .ToListAsync();

        var summary = students.Select(s => new AttendanceSummaryRow
        {
            StudentId   = s.Id,
            StudentName = s.DisplayName ?? s.Email ?? "Student",
            Present     = records.Count(a => a.StudentId == s.Id && a.Status == AttendanceStatus.Present),
            Late        = records.Count(a => a.StudentId == s.Id && a.Status == AttendanceStatus.Late),
            Absent      = records.Count(a => a.StudentId == s.Id && a.Status == AttendanceStatus.Absent),
            Total       = records.Where(a => a.StudentId == s.Id).Select(a => a.Date.Date).Distinct().Count()
        }).ToList();

        ViewBag.CourseName = course.Name;
        ViewBag.CourseId   = courseId;
        ViewBag.Dates      = records.Select(a => a.Date.Date).Distinct().OrderBy(d => d).ToList();
        ViewBag.Records    = records;
        return View(summary);
    }

    // ─── STUDENT: My Attendance ───────────────────────────────────────────────

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> MyAttendance(int? courseId)
    {
        var studentId = _userManager.GetUserId(User)!;

        var courses = await _context.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
            .Select(e => e.Course!)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Courses   = courses.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
        ViewBag.CourseId  = courseId;

        List<Attendance> records = new();
        if (courseId.HasValue)
        {
            records = await _context.Attendances.AsNoTracking()
                .Where(a => a.StudentId == studentId && a.CourseId == courseId.Value)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        return View(records);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<bool> OwnsCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
    }
}

// ─── View Model ──────────────────────────────────────────────────────────────

public class AttendanceSummaryRow
{
    public string StudentId   { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int    Present     { get; set; }
    public int    Late        { get; set; }
    public int    Absent      { get; set; }
    public int    Total       { get; set; }

    public decimal AttendancePercent =>
        Total == 0 ? 0 : Math.Round((Present + Late) * 100m / Total, 1);
}
