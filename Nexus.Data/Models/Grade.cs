using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Grade
{
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal AssignmentMarks { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal MidtermMarks { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal FinalMarks { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal TotalMarks { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal QuizMarks { get; set; }

    public void RecalculateTotal(
        decimal assignmentWeight = 20m,
        decimal midtermWeight    = 30m,
        decimal finalWeight      = 50m,
        decimal quizWeight       = 0m)
    {
        TotalMarks = Math.Round(
            (AssignmentMarks * assignmentWeight / 100m) +
            (MidtermMarks    * midtermWeight    / 100m) +
            (FinalMarks      * finalWeight      / 100m) +
            (QuizMarks       * quizWeight       / 100m), 2);
    }
}
