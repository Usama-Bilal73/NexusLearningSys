using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nexus.Data.Models;

namespace Nexus.Web.ViewModels.Repository;

public class DocumentSearchViewModel
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public int? CourseId { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<DocumentListItemViewModel> Documents { get; set; } = [];
}

public class DocumentListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public CourseMaterialType MaterialType { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? Summary { get; set; }
}

public class DocumentUploadViewModel
{
    [Required] public int CourseId { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Category { get; set; } = "General";
    [Required] public CourseMaterialType MaterialType { get; set; } = CourseMaterialType.CourseMaterial;
    [Required] public IFormFile? File { get; set; }
    public bool GenerateAiSummary { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
}

public class CourseAssistantChatViewModel
{
    [Required] public int CourseId { get; set; }
    [Required, StringLength(1000)] public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public IEnumerable<SelectListItem> Courses { get; set; } = [];
    public IReadOnlyList<string> Sources { get; set; } = [];
}
