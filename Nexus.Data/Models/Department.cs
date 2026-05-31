using System.ComponentModel.DataAnnotations;

namespace Nexus.Data.Models;

public class Department
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
