using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Web.Services;

namespace Nexus.Web.Controllers;

[Authorize]
public class TranscriptController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGpaCalculationService _gpa;

    public TranscriptController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IGpaCalculationService gpa)
    {
        _context = context;
        _userManager = userManager;
        _gpa = gpa;
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> MyTranscript()
    {
        var studentId = _userManager.GetUserId(User)!;
        var student   = await _userManager.FindByIdAsync(studentId);

        var grades = await _context.Grades.AsNoTracking()
            .Include(g => g.Course).ThenInclude(c => c!.AcademicSemester)
            .Where(g => g.StudentId == studentId)
            .OrderBy(g => g.Course!.AcademicSemester != null ? g.Course.AcademicSemester.StartDate : DateTime.MinValue)
            .ThenBy(g => g.Course!.Name)
            .ToListAsync();

        var rows = grades.Select(g => new TranscriptRowViewModel
        {
            CourseName       = g.Course?.Name ?? "—",
            Semester         = g.Course?.AcademicSemester?.Name ?? "—",
            AssignmentMarks  = g.AssignmentMarks,
            MidtermMarks     = g.MidtermMarks,
            FinalMarks       = g.FinalMarks,
            QuizMarks        = g.QuizMarks,
            TotalMarks       = g.TotalMarks,
            LetterGrade      = _gpa.GetLetterGrade(g.TotalMarks),
            GpaPoints        = _gpa.GetGpa(g.TotalMarks)
        }).ToList();

        var cgpa = await _gpa.GetStudentCgpaAsync(studentId);

        var vm = new TranscriptViewModel
        {
            StudentName = student?.FullName ?? student?.Email ?? "Student",
            Email       = student?.Email ?? "",
            Rows        = rows,
            Cgpa        = cgpa
        };

        return View(vm);
    }

    // Admin or Teacher can view any student's transcript
    [Authorize(Roles = $"{ApplicationRoles.Admin},{ApplicationRoles.Teacher}")]
    public async Task<IActionResult> StudentView(string studentId)
    {
        var student = await _userManager.FindByIdAsync(studentId);
        if (student is null) return NotFound();

        var grades = await _context.Grades.AsNoTracking()
            .Include(g => g.Course).ThenInclude(c => c!.AcademicSemester)
            .Where(g => g.StudentId == studentId)
            .OrderBy(g => g.Course!.Name)
            .ToListAsync();

        var rows = grades.Select(g => new TranscriptRowViewModel
        {
            CourseName       = g.Course?.Name ?? "—",
            Semester         = g.Course?.AcademicSemester?.Name ?? "—",
            AssignmentMarks  = g.AssignmentMarks,
            MidtermMarks     = g.MidtermMarks,
            FinalMarks       = g.FinalMarks,
            QuizMarks        = g.QuizMarks,
            TotalMarks       = g.TotalMarks,
            LetterGrade      = _gpa.GetLetterGrade(g.TotalMarks),
            GpaPoints        = _gpa.GetGpa(g.TotalMarks)
        }).ToList();

        var cgpa = await _gpa.GetStudentCgpaAsync(studentId);

        var vm = new TranscriptViewModel
        {
            StudentName = student.FullName,
            Email       = student.Email ?? "",
            Rows        = rows,
            Cgpa        = cgpa
        };

        return View("MyTranscript", vm);
    }
}

// ─── View Models ─────────────────────────────────────────────────────────────

public class TranscriptViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public List<TranscriptRowViewModel> Rows { get; set; } = new();
    public decimal Cgpa { get; set; }
}

public class TranscriptRowViewModel
{
    public string  CourseName      { get; set; } = string.Empty;
    public string  Semester        { get; set; } = string.Empty;
    public decimal AssignmentMarks { get; set; }
    public decimal MidtermMarks    { get; set; }
    public decimal FinalMarks      { get; set; }
    public decimal QuizMarks       { get; set; }
    public decimal TotalMarks      { get; set; }
    public string  LetterGrade     { get; set; } = string.Empty;
    public decimal GpaPoints       { get; set; }
}
