using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Submission
{
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    public int AssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
