using System.ComponentModel.DataAnnotations;
using Nexus.Data.Identity;

namespace Nexus.Data.Models;

public enum CourseMaterialType
{
    CourseMaterial = 0,
    LectureNotes = 1,
    LabManual = 2,
    Syllabus = 3
}

public class CourseMaterial
{
    public int Id { get; set; }

    [Required]
    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    public CourseMaterialType MaterialType { get; set; }

    [Required]
    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContentType { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string UploadedByTeacherId { get; set; } = string.Empty;

    public ApplicationUser? UploadedByTeacher { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }
}
