using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nexus.Data.Models;

namespace Nexus.Web.ViewModels.Admin;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Course name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Semester { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Teacher")]
    public string TeacherId { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    public IEnumerable<SelectListItem> Teachers { get; set; } = [];
    public IEnumerable<SelectListItem> Departments { get; set; } = [];

    public static CourseFormViewModel FromEntity(Course course) => new()
    {
        Id = course.Id,
        Name = course.Name,
        Semester = course.Semester,
        TeacherId = course.TeacherId,
        DepartmentId = course.DepartmentId
    };
}

public class CourseListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = "Unassigned";
}
