using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nexus.Web.ViewModels.Quiz;

public class QuizFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Range(1, 240)] public int DurationMinutes { get; set; } = 15;
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public bool IsPublished { get; set; }
    [Range(0, 10)] public int MaxAttempts { get; set; } = 1;
    public bool ShuffleQuestions { get; set; }
    [Required] public int CourseId { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}

public class QuestionFormViewModel
{
    public int QuizId { get; set; }
    [Required, StringLength(1000)] public string Text { get; set; } = string.Empty;
    [Required, StringLength(400)] public string OptionA { get; set; } = string.Empty;
    [Required, StringLength(400)] public string OptionB { get; set; } = string.Empty;
    [Required, StringLength(400)] public string OptionC { get; set; } = string.Empty;
    [Required, StringLength(400)] public string OptionD { get; set; } = string.Empty;
    [Required, RegularExpression("^[A-D]$")] public string CorrectOption { get; set; } = "A";
    [Range(0.5, 100)] public decimal Points { get; set; } = 1;
}

public class QuizAttemptViewModel
{
    public int QuizId { get; set; }
    public int AttemptId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int RemainingSeconds { get; set; }
    /// <summary>-1 means unlimited. 0 means no more attempts.</summary>
    public int AttemptsRemaining { get; set; } = -1;
    public IReadOnlyList<QuestionAttemptViewModel> Questions { get; set; } = [];
    public Dictionary<int, string?> Answers { get; set; } = [];
}

public class QuestionAttemptViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
}
