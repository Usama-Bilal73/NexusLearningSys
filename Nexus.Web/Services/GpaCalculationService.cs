using Microsoft.EntityFrameworkCore;
using Nexus.Data.Persistence;

namespace Nexus.Web.Services;

public interface IGpaCalculationService
{
    decimal GetGpa(decimal totalMarks);
    string GetLetterGrade(decimal totalMarks);
    Task<decimal> GetStudentCgpaAsync(string studentId, CancellationToken cancellationToken = default);
}

public class GpaCalculationService : IGpaCalculationService
{
    private readonly ApplicationDbContext _context;

    public GpaCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Converts a percentage total to a 4.0 GPA value.</summary>
    public decimal GetGpa(decimal totalMarks) => totalMarks switch
    {
        >= 90 => 4.0m,
        >= 85 => 3.7m,
        >= 80 => 3.3m,
        >= 75 => 3.0m,
        >= 70 => 2.7m,
        >= 65 => 2.3m,
        >= 60 => 2.0m,
        >= 55 => 1.7m,
        >= 50 => 1.3m,
        >= 45 => 1.0m,
        _     => 0.0m
    };

    /// <summary>Returns letter grade based on percentage marks.</summary>
    public string GetLetterGrade(decimal totalMarks) => totalMarks switch
    {
        >= 90 => "A+",
        >= 85 => "A",
        >= 80 => "A-",
        >= 75 => "B+",
        >= 70 => "B",
        >= 65 => "B-",
        >= 60 => "C+",
        >= 55 => "C",
        >= 50 => "C-",
        >= 45 => "D",
        _     => "F"
    };

    /// <summary>Calculates cumulative GPA across all graded courses for a student.</summary>
    public async Task<decimal> GetStudentCgpaAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var grades = await _context.Grades.AsNoTracking()
            .Where(g => g.StudentId == studentId && g.TotalMarks > 0)
            .Select(g => g.TotalMarks)
            .ToListAsync(cancellationToken);

        if (grades.Count == 0) return 0m;
        return Math.Round(grades.Average(t => GetGpa(t)), 2);
    }
}
