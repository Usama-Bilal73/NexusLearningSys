using System.ComponentModel.DataAnnotations;

namespace Nexus.Data.Models;

public class Assignment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
