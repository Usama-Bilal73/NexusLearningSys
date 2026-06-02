using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Data.Models;

public class GradeWeight
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal AssignmentWeight { get; set; } = 20m;

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal MidtermWeight { get; set; } = 30m;

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal FinalWeight { get; set; } = 50m;

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal QuizWeight { get; set; } = 0m;

    /// <summary>Returns true if weights sum to 100.</summary>
    public bool IsValid => (AssignmentWeight + MidtermWeight + FinalWeight + QuizWeight) == 100m;
}
