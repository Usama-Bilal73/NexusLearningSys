using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public enum NotificationType
{
    System = 0,
    AssignmentDue = 1,
    QuizReminder = 2,
    CourseAnnouncement = 3
}

public class Notification
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string RecipientUserId { get; set; } = string.Empty;

    public ApplicationUser? RecipientUser { get; set; }

    [MaxLength(450)]
    public string? SenderUserId { get; set; }

    public ApplicationUser? SenderUser { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    public NotificationType NotificationType { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
