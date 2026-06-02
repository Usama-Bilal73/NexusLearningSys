using Microsoft.EntityFrameworkCore;
using Nexus.Data.Models;
using Nexus.Data.Persistence;

namespace Nexus.Web.Services;

public interface IGradebookService
{
    Task<GradeWeight> GetWeightsOrDefaultAsync(int courseId, CancellationToken cancellationToken = default);
    Task SaveWeightsAsync(GradeWeight weights, CancellationToken cancellationToken = default);
    Task<Grade> UpsertGradeAsync(string studentId, int courseId, decimal assignmentMarks, decimal midtermMarks, decimal finalMarks, CancellationToken cancellationToken = default);
    Task RecalculateAllGradesForCourseAsync(int courseId, CancellationToken cancellationToken = default);
    Task UpdateStudentQuizMarksAndTotalAsync(string studentId, int courseId, CancellationToken cancellationToken = default);
}

public class GradebookService : IGradebookService
{
    private readonly ApplicationDbContext _context;

    public GradebookService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GradeWeight> GetWeightsOrDefaultAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var weights = await _context.GradeWeights.FirstOrDefaultAsync(w => w.CourseId == courseId, cancellationToken);
        if (weights is null)
        {
            weights = new GradeWeight
            {
                CourseId = courseId,
                AssignmentWeight = 20m,
                MidtermWeight = 30m,
                FinalWeight = 50m,
                QuizWeight = 0m
            };
        }
        return weights;
    }

    public async Task SaveWeightsAsync(GradeWeight weights, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GradeWeights.FirstOrDefaultAsync(w => w.CourseId == weights.CourseId, cancellationToken);
        if (existing is null)
        {
            _context.GradeWeights.Add(weights);
        }
        else
        {
            existing.AssignmentWeight = weights.AssignmentWeight;
            existing.MidtermWeight = weights.MidtermWeight;
            existing.FinalWeight = weights.FinalWeight;
            existing.QuizWeight = weights.QuizWeight;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

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
        
        // Compute quiz marks
        grade.QuizMarks = await CalculateQuizMarksAsync(studentId, courseId, cancellationToken);

        // Recalculate using course weights
        var weights = await GetWeightsOrDefaultAsync(courseId, cancellationToken);
        grade.RecalculateTotal(weights.AssignmentWeight, weights.MidtermWeight, weights.FinalWeight, weights.QuizWeight);

        await _context.SaveChangesAsync(cancellationToken);
        return grade;
    }

    public async Task RecalculateAllGradesForCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var weights = await GetWeightsOrDefaultAsync(courseId, cancellationToken);
        var grades = await _context.Grades.Where(g => g.CourseId == courseId).ToListAsync(cancellationToken);

        foreach (var grade in grades)
        {
            grade.QuizMarks = await CalculateQuizMarksAsync(grade.StudentId, courseId, cancellationToken);
            grade.RecalculateTotal(weights.AssignmentWeight, weights.MidtermWeight, weights.FinalWeight, weights.QuizWeight);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStudentQuizMarksAndTotalAsync(string studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
        if (!isEnrolled) return;

        var grade = await _context.Grades.FirstOrDefaultAsync(g => g.StudentId == studentId && g.CourseId == courseId, cancellationToken);
        if (grade is null)
        {
            grade = new Grade { StudentId = studentId, CourseId = courseId };
            _context.Grades.Add(grade);
        }

        grade.QuizMarks = await CalculateQuizMarksAsync(studentId, courseId, cancellationToken);
        var weights = await GetWeightsOrDefaultAsync(courseId, cancellationToken);
        grade.RecalculateTotal(weights.AssignmentWeight, weights.MidtermWeight, weights.FinalWeight, weights.QuizWeight);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> CalculateQuizMarksAsync(string studentId, int courseId, CancellationToken cancellationToken)
    {
        var quizzes = await _context.Quizzes
            .Where(q => q.CourseId == courseId && q.IsPublished)
            .Include(q => q.Questions)
            .ToListAsync(cancellationToken);

        if (quizzes.Count == 0) return 0m;

        decimal totalQuizPercentageSum = 0m;
        int quizzesWithScoresCount = 0;

        foreach (var quiz in quizzes)
        {
            var maxQuizPoints = quiz.Questions.Sum(q => q.Points);
            if (maxQuizPoints == 0) continue;

            var attempts = await _context.QuizAttempts
                .Where(a => a.QuizId == quiz.Id && a.StudentId == studentId && a.SubmittedAtUtc != null)
                .ToListAsync(cancellationToken);

            if (attempts.Any())
            {
                var maxScore = attempts.Max(a => a.Score);
                var percentage = (maxScore / (decimal)maxQuizPoints) * 100m;
                totalQuizPercentageSum += percentage;
                quizzesWithScoresCount++;
            }
        }

        if (quizzesWithScoresCount == 0) return 0m;
        return Math.Round(totalQuizPercentageSum / quizzesWithScoresCount, 2);
    }
}
