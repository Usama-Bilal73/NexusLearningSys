using Microsoft.EntityFrameworkCore;
using Nexus.Data.Models;
using Nexus.Data.Persistence;

namespace Nexus.Web.Services;

public interface IGradebookService
{
    decimal CalculateTotal(decimal assignmentMarks, decimal midtermMarks, decimal finalMarks);
    Task<Grade> UpsertGradeAsync(string studentId, int courseId, decimal assignmentMarks, decimal midtermMarks, decimal finalMarks, CancellationToken cancellationToken = default);
}

public class GradebookService : IGradebookService
{
    private readonly ApplicationDbContext _context;

    public GradebookService(ApplicationDbContext context)
    {
        _context = context;
    }

    public decimal CalculateTotal(decimal assignmentMarks, decimal midtermMarks, decimal finalMarks)
        => Math.Round((assignmentMarks * 0.20m) + (midtermMarks * 0.30m) + (finalMarks * 0.50m), 2);

    public async Task<Grade> UpsertGradeAsync(string studentId, int courseId, decimal assignmentMarks, decimal midtermMarks, decimal finalMarks, CancellationToken cancellationToken = default)
    {
        var grade = await _context.Grades.FirstOrDefaultAsync(item => item.StudentId == studentId && item.CourseId == courseId, cancellationToken);
        if (grade is null)
        {
            grade = new Grade { StudentId = studentId, CourseId = courseId };
            _context.Grades.Add(grade);
        }

        grade.AssignmentMarks = assignmentMarks;
        grade.MidtermMarks = midtermMarks;
        grade.FinalMarks = finalMarks;
        grade.TotalMarks = CalculateTotal(assignmentMarks, midtermMarks, finalMarks);
        await _context.SaveChangesAsync(cancellationToken);
        return grade;
    }
}
