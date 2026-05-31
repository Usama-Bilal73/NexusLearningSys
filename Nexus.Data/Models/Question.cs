using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexus.Data.Models;

public class Question
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-D]$")]
    [MaxLength(1)]
    public string CorrectOption { get; set; } = "A";

    [Column(TypeName = "decimal(5,2)")]
    [Range(0.5, 100)]
    public decimal Points { get; set; } = 1;

    public int QuizId { get; set; }

    public Quiz? Quiz { get; set; }
}
