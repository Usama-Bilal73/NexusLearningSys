using System.ComponentModel.DataAnnotations;

namespace Nexus.Data.Models;

public class Quiz
{
    public int Id { get; set; }

    [Required]
    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 240)]
    public int DurationMinutes { get; set; } = 15;

    public DateTime? OpensAtUtc { get; set; }

    public DateTime? ClosesAtUtc { get; set; }

    public bool IsPublished { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
