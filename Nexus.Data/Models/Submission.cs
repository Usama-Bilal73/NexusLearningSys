using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Submission
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int AssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = "Submitted";

    [MaxLength(1000)]
    public string? Feedback { get; set; }
}
