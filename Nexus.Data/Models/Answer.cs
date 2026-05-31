using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Data.Models;

public class Answer
{
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }

    public QuizAttempt? QuizAttempt { get; set; }

    public int QuestionId { get; set; }

    public Question? Question { get; set; }

    [MaxLength(1)]
    [RegularExpression("^[A-D]$")]
    public string? SelectedOption { get; set; }

    public bool IsCorrect { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PointsEarned { get; set; }
}
