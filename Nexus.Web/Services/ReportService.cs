using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Models;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nexus.Web.Services;

public interface IReportService
{
    Task<byte[]> GenerateTranscriptPdfAsync(string studentId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateGradebookExcelAsync(int courseId, string teacherId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateAttendanceReportPdfAsync(int courseId, string teacherId, CancellationToken cancellationToken = default);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IGpaCalculationService _gpa;

    public ReportService(ApplicationDbContext context, IGpaCalculationService gpa)
    {
        _context = context;
        _gpa = gpa;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ─── PDF TRANSCRIPT ───────────────────────────────────────────────────────

    public async Task<byte[]> GenerateTranscriptPdfAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken);
        var grades = await _context.Grades.AsNoTracking()
            .Include(g => g.Course).ThenInclude(c => c!.AcademicSemester)
            .Where(g => g.StudentId == studentId)
            .OrderBy(g => g.Course!.Name)
            .ToListAsync(cancellationToken);

        var cgpa = await _gpa.GetStudentCgpaAsync(studentId, cancellationToken);
        var studentName = student?.DisplayName ?? student?.Email ?? "Student";

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("NEXUS LEARNING SYSTEM").Bold().FontSize(18).FontColor("#1a1a2e");
                    col.Item().AlignCenter().Text("Official Academic Transcript").FontSize(12).FontColor("#4a4a6a");
                    col.Item().Height(8);
                    col.Item().LineHorizontal(1).LineColor("#cccccc");
                    col.Item().Height(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Student: {studentName}").Bold();
                        row.RelativeItem().AlignRight().Text($"Generated: {DateTime.Now:dd MMM yyyy}");
                    });
                    col.Item().Text($"Email: {student?.Email ?? "—"}");
                    col.Item().Height(12);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        // Header
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1a1a2e").Padding(6);

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Course", "Semester", "Assign.", "Midterm", "Final", "Total", "Grade", "GPA" })
                                header.Cell().Element(HeaderCell).Text(h).FontColor(Colors.White).Bold().FontSize(9);
                        });

                        // Rows
                        var even = false;
                        foreach (var g in grades)
                        {
                            even = !even;
                            string bg = even ? "#f8f8ff" : "#ffffff";
                            IContainer Cell(IContainer c) => c.Background(bg).Padding(5);

                            var letter = _gpa.GetLetterGrade(g.TotalMarks);
                            var gpaVal = _gpa.GetGpa(g.TotalMarks);

                            table.Cell().Element(Cell).Text(g.Course?.Name ?? "—").FontSize(9);
                            table.Cell().Element(Cell).Text(g.Course?.AcademicSemester?.Name ?? "—").FontSize(9);
                            table.Cell().Element(Cell).Text($"{g.AssignmentMarks:0.##}").FontSize(9);
                            table.Cell().Element(Cell).Text($"{g.MidtermMarks:0.##}").FontSize(9);
                            table.Cell().Element(Cell).Text($"{g.FinalMarks:0.##}").FontSize(9);
                            table.Cell().Element(Cell).Text($"{g.TotalMarks:0.##}").Bold().FontSize(9);
                            table.Cell().Element(Cell).Text(letter).Bold().FontSize(9)
                                .FontColor(letter.StartsWith("F") ? Colors.Red.Medium : Colors.Green.Darken2);
                            table.Cell().Element(Cell).Text($"{gpaVal:0.0}").FontSize(9);
                        }
                    });

                    col.Item().Height(20);
                    col.Item().AlignRight().Text($"CGPA: {cgpa:0.00}").Bold().FontSize(14).FontColor("#1a1a2e");
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" — Nexus Learning System — Confidential");
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // ─── EXCEL GRADEBOOK ──────────────────────────────────────────────────────

    public async Task<byte[]> GenerateGradebookExcelAsync(int courseId, string teacherId, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId, cancellationToken);
        if (course is null) return Array.Empty<byte>();

        var enrollments = await _context.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Student!.DisplayName)
            .ToListAsync(cancellationToken);

        var grades = await _context.Grades.AsNoTracking()
            .Where(g => g.CourseId == courseId)
            .ToDictionaryAsync(g => g.StudentId, cancellationToken);

        var weights = await _context.GradeWeights.AsNoTracking()
            .FirstOrDefaultAsync(w => w.CourseId == courseId, cancellationToken);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(course.Name.Length > 30 ? course.Name[..30] : course.Name);

        // Title
        ws.Cell(1, 1).Value = $"Gradebook — {course.Name}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
        ws.Cell(3, 1).Value = weights is not null
            ? $"Weights — Assignment:{weights.AssignmentWeight}% | Midterm:{weights.MidtermWeight}% | Final:{weights.FinalWeight}% | Quiz:{weights.QuizWeight}%"
            : "Weights — Assignment:20% | Midterm:30% | Final:50%";

        // Header row
        int headerRow = 5;
        string[] headers = { "No.", "Student Name", "Email", "Assignment", "Midterm", "Final", "Quiz", "Total", "Grade", "GPA" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = headerRow + 1;
        int no = 1;
        foreach (var enrollment in enrollments)
        {
            grades.TryGetValue(enrollment.StudentId, out var grade);
            var total   = grade?.TotalMarks ?? 0;
            var letter  = _gpa.GetLetterGrade(total);
            var gpaVal  = _gpa.GetGpa(total);

            ws.Cell(row, 1).Value  = no++;
            ws.Cell(row, 2).Value  = enrollment.Student?.DisplayName ?? enrollment.Student?.Email ?? "Student";
            ws.Cell(row, 3).Value  = enrollment.Student?.Email ?? "";
            ws.Cell(row, 4).Value  = (double)(grade?.AssignmentMarks ?? 0);
            ws.Cell(row, 5).Value  = (double)(grade?.MidtermMarks ?? 0);
            ws.Cell(row, 6).Value  = (double)(grade?.FinalMarks ?? 0);
            ws.Cell(row, 7).Value  = (double)(grade?.QuizMarks ?? 0);
            ws.Cell(row, 8).Value  = (double)total;
            ws.Cell(row, 9).Value  = letter;
            ws.Cell(row, 10).Value = (double)gpaVal;

            ws.Cell(row, 8).Style.Font.Bold = true;
            if (letter == "F") ws.Cell(row, 9).Style.Font.FontColor = XLColor.Red;

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Range(headerRow, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(headerRow, 1, row - 1, headers.Length).Style.Border.InsideBorder  = XLBorderStyleValues.Hair;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── PDF ATTENDANCE REPORT ────────────────────────────────────────────────

    public async Task<byte[]> GenerateAttendanceReportPdfAsync(int courseId, string teacherId, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId, cancellationToken);
        if (course is null) return Array.Empty<byte>();

        var students = await _context.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId)
            .Select(e => e.Student!)
            .OrderBy(s => s.DisplayName)
            .ToListAsync(cancellationToken);

        var records = await _context.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("NEXUS LEARNING SYSTEM — ATTENDANCE REPORT").Bold().FontSize(14).FontColor("#1a1a2e");
                    col.Item().AlignCenter().Text($"Course: {course.Name}  |  Generated: {DateTime.Now:dd MMM yyyy}").FontSize(10);
                    col.Item().Height(8);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3); // Name
                            cols.RelativeColumn(1); // Present
                            cols.RelativeColumn(1); // Late
                            cols.RelativeColumn(1); // Absent
                            cols.RelativeColumn(1); // Total
                            cols.RelativeColumn(1); // %
                        });

                        IContainer HCell(IContainer c) => c.Background("#1a1a2e").Padding(6);

                        table.Header(h =>
                        {
                            foreach (var hdr in new[] { "Student", "Present", "Late", "Absent", "Total Days", "Attendance %" })
                                h.Cell().Element(HCell).Text(hdr).FontColor(Colors.White).Bold().FontSize(8);
                        });

                        var even = false;
                        foreach (var student in students)
                        {
                            even = !even;
                            var bgColor = even ? "#f8f8ff" : "#ffffff";
                            IContainer Cell(IContainer c) => c.Background(bgColor).Padding(5);

                            var present = records.Count(a => a.StudentId == student.Id && a.Status == AttendanceStatus.Present);
                            var late    = records.Count(a => a.StudentId == student.Id && a.Status == AttendanceStatus.Late);
                            var absent  = records.Count(a => a.StudentId == student.Id && a.Status == AttendanceStatus.Absent);
                            var total   = records.Where(a => a.StudentId == student.Id).Select(a => a.Date.Date).Distinct().Count();
                            var pct     = total == 0 ? 0 : Math.Round((present + late) * 100m / total, 1);

                            table.Cell().Element(Cell).Text(student.DisplayName ?? student.Email ?? "Student");
                            table.Cell().Element(Cell).Text($"{present}").FontColor(Colors.Green.Darken2);
                            table.Cell().Element(Cell).Text($"{late}").FontColor(Colors.Orange.Medium);
                            table.Cell().Element(Cell).Text($"{absent}").FontColor(Colors.Red.Medium);
                            table.Cell().Element(Cell).Text($"{total}").Bold();
                            table.Cell().Element(Cell).Text($"{pct}%").Bold()
                                .FontColor(pct >= 75 ? Colors.Green.Darken2 : Colors.Red.Medium);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" — Nexus Learning System");
                });
            });
        });

        return pdf.GeneratePdf();
    }
}
