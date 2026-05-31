using System.ComponentModel.DataAnnotations.Schema;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class QuizAttempt
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAtUtc { get; set; }

    public bool IsAutoSubmitted { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Score { get; set; }

    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
