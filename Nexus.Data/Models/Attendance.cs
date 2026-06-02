using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public class Attendance
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = AttendanceStatus.Present;

    public string MarkedByTeacherId { get; set; } = string.Empty;
    public ApplicationUser? MarkedByTeacher { get; set; }

    public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class AttendanceStatus
{
    public const string Present = "Present";
    public const string Absent  = "Absent";
    public const string Late    = "Late";

    public static readonly string[] All = [Present, Absent, Late];
}
