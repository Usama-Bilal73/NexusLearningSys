using System.ComponentModel.DataAnnotations;
using Nexus.Data.Models;

namespace Nexus.Web.ViewModels.Admin;

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Department name")]
    public string Name { get; set; } = string.Empty;

    public static DepartmentFormViewModel FromEntity(Department department) => new() { Id = department.Id, Name = department.Name };
}
