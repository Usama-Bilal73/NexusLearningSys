using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Web.Services;

namespace Nexus.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService reports, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _reports = reports;
        _context = context;
        _userManager = userManager;
    }

    // ─── STUDENT: Download own PDF transcript ─────────────────────────────────

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> DownloadTranscript(CancellationToken cancellationToken)
    {
        var studentId = _userManager.GetUserId(User)!;
        var pdf = await _reports.GenerateTranscriptPdfAsync(studentId, cancellationToken);
        return File(pdf, "application/pdf", $"Transcript_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // ─── TEACHER: Download Excel gradebook for a course ───────────────────────

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> DownloadGradebook(int courseId, CancellationToken cancellationToken)
    {
        var teacherId = _userManager.GetUserId(User)!;
        if (!await OwnsCourseAsync(courseId, teacherId)) return Forbid();

        var excel = await _reports.GenerateGradebookExcelAsync(courseId, teacherId, cancellationToken);
        if (excel.Length == 0) return NotFound();
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Gradebook_{courseId}_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ─── TEACHER: Download PDF attendance report for a course ─────────────────

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> DownloadAttendance(int courseId, CancellationToken cancellationToken)
    {
        var teacherId = _userManager.GetUserId(User)!;
        if (!await OwnsCourseAsync(courseId, teacherId)) return Forbid();

        var pdf = await _reports.GenerateAttendanceReportPdfAsync(courseId, teacherId, cancellationToken);
        if (pdf.Length == 0) return NotFound();
        return File(pdf, "application/pdf", $"Attendance_{courseId}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // ─── ADMIN: Download any student's transcript ─────────────────────────────

    [Authorize(Roles = ApplicationRoles.Admin)]
    public async Task<IActionResult> DownloadStudentTranscript(string studentId, CancellationToken cancellationToken)
    {
        var pdf = await _reports.GenerateTranscriptPdfAsync(studentId, cancellationToken);
        return File(pdf, "application/pdf", $"Transcript_{studentId[..8]}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    private async Task<bool> OwnsCourseAsync(int courseId, string teacherId)
        => await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
}
