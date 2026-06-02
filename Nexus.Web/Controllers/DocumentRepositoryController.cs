using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;
using Nexus.Web.Services;
using Nexus.Web.ViewModels.Repository;
using Nexus.Web.ViewModels.Teacher;

namespace Nexus.Web.Controllers;

[Authorize]
public class DocumentRepositoryController : Controller
{
    private static readonly HashSet<string> RepositoryExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".pptx" };
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IOpenAiLearningService _openAiLearningService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentRepositoryController(ApplicationDbContext context, IFileStorageService fileStorage, IPdfTextExtractor pdfTextExtractor, IOpenAiLearningService openAiLearningService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileStorage = fileStorage;
        _pdfTextExtractor = pdfTextExtractor;
        _openAiLearningService = openAiLearningService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(DocumentSearchViewModel model)
    {
        try
        {
            await EnsureRepositoryColumnsAsync();
        }
        catch
        {
            // ignore - we'll surface DB errors below if necessary
        }
        var query = _context.CourseMaterials.AsNoTracking().Include(m => m.Course).AsQueryable();
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacherId = _userManager.GetUserId(User)!;
            query = query.Where(m => m.Course!.TeacherId == teacherId);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            var studentId = _userManager.GetUserId(User)!;
            query = query.Where(m => m.Course!.Enrollments.Any(e => e.StudentId == studentId));
        }

        if (model.CourseId.HasValue) query = query.Where(m => m.CourseId == model.CourseId.Value);
        if (!string.IsNullOrWhiteSpace(model.Category)) query = query.Where(m => m.Category == model.Category);
        if (!string.IsNullOrWhiteSpace(model.Query))
        {
            var term = model.Query.Trim();
            query = query.Where(m => m.Title.Contains(term) || m.OriginalFileName.Contains(term) || m.Category.Contains(term) || (m.AiSummary != null && m.AiSummary.Contains(term)));
        }

        model.Documents = await query.OrderByDescending(m => m.UploadedAtUtc).Select(m => new DocumentListItemViewModel
        {
            Id = m.Id,
            Title = m.Title,
            CourseName = m.Course!.Name,
            Category = m.Category,
            FileName = m.OriginalFileName,
            MaterialType = m.MaterialType,
            UploadedAtUtc = m.UploadedAtUtc,
            Summary = m.AiSummary
        }).ToListAsync();
        await PopulateFiltersAsync(model);
        return View(model);
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Upload() => View(await PopulateCoursesAsync(new DocumentUploadViewModel()));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureRepositoryColumnsAsync();
        }
        catch
        {
            // ignore
        }
        if (!await OwnsCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select one of your courses.");
        if (model.File is null) ModelState.AddModelError(nameof(model.File), "Select a file.");
        else if (!RepositoryExtensions.Contains(Path.GetExtension(model.File.FileName))) ModelState.AddModelError(nameof(model.File), "Only PDF, DOCX, and PPTX files are allowed in the repository.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));

        try
        {
            var path = await _fileStorage.SaveUploadAsync(model.File!, "document-repository", cancellationToken);
            var material = new CourseMaterial
            {
                CourseId = model.CourseId,
                Title = model.Title.Trim(),
                Category = model.Category.Trim(),
                MaterialType = model.MaterialType,
                OriginalFileName = model.File!.FileName,
                ContentType = model.File.ContentType,
                FilePath = path,
                UploadedByTeacherId = _userManager.GetUserId(User)!
            };

            if (Path.GetExtension(model.File.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                material.ExtractedText = await _pdfTextExtractor.ExtractTextAsync(path, cancellationToken);
                if (model.GenerateAiSummary && !string.IsNullOrWhiteSpace(material.ExtractedText))
                {
                    material.AiSummary = await _openAiLearningService.SummarizeAsync(material.ExtractedText, cancellationToken);
                    material.SummarizedAtUtc = DateTime.UtcNow;
                }
            }

            _context.CourseMaterials.Add(material);
            await _context.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Document uploaded.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.File), ex.Message);
            return View(await PopulateCoursesAsync(model));
        }
    }

    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.CourseMaterials.Include(m => m.Course).ThenInclude(c => c!.Enrollments).FirstOrDefaultAsync(m => m.Id == id);
        if (document is null) return NotFound();
        if (!CanAccess(document)) return Forbid();
        return Redirect(document.FilePath);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> GenerateSummary(int id, CancellationToken cancellationToken)
    {
        var teacherId = _userManager.GetUserId(User)!;
        var document = await _context.CourseMaterials.Include(m => m.Course).FirstOrDefaultAsync(m => m.Id == id && m.Course!.TeacherId == teacherId, cancellationToken);
        if (document is null) return NotFound();
        if (!Path.GetExtension(document.OriginalFileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "AI summarization is available for PDF files.";
            return RedirectToAction(nameof(Index));
        }

        document.ExtractedText = string.IsNullOrWhiteSpace(document.ExtractedText) ? await _pdfTextExtractor.ExtractTextAsync(document.FilePath, cancellationToken) : document.ExtractedText;
        document.AiSummary = await _openAiLearningService.SummarizeAsync(document.ExtractedText ?? string.Empty, cancellationToken);
        document.SummarizedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "AI summary generated.";
        return RedirectToAction(nameof(Index));
    }

    private bool CanAccess(CourseMaterial document)
    {
        if (User.IsInRole(ApplicationRoles.Teacher)) return document.Course?.TeacherId == _userManager.GetUserId(User);
        if (User.IsInRole(ApplicationRoles.Student)) return document.Course?.Enrollments.Any(e => e.StudentId == _userManager.GetUserId(User)) == true;
        return User.IsInRole(ApplicationRoles.Admin);
    }

    private async Task<bool> OwnsCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Courses.AnyAsync(course => course.Id == courseId && course.TeacherId == teacherId);
    }

    private async Task<T> PopulateCoursesAsync<T>(T model) where T : class
    {
        var teacherId = _userManager.GetUserId(User)!;
        var courses = await _context.Courses.AsNoTracking().Where(course => course.TeacherId == teacherId).OrderBy(course => course.Name).ToListAsync();
        var items = courses.Select(course => new SelectListItem($"{course.Name} ({course.Semester})", course.Id.ToString())).ToList();
        switch (model)
        {
            case DocumentUploadViewModel docUpload: docUpload.Courses = items; break;
            case AssignmentFormViewModel assignment: assignment.Courses = items; break;
            case MaterialUploadViewModel material: material.Courses = items; break;
        }
        return model;
    }

    public async Task<IActionResult> CreateAssignment(int? courseId)
    {
        var model = await PopulateCoursesAsync(new AssignmentFormViewModel());
        if (courseId.HasValue) model.CourseId = courseId.Value;
        return View(model);
    }

    public async Task<IActionResult> UploadMaterial(int? courseId)
    {
        var model = await PopulateCoursesAsync(new MaterialUploadViewModel());
        if (courseId.HasValue) model.CourseId = courseId.Value;
        return View(model);
    }

    private async Task EnsureRepositoryColumnsAsync()
    {
        var sql = @"IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'Category') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials]
    ADD [Category] nvarchar(80) NOT NULL
        CONSTRAINT [DF_CourseMaterials_Category] DEFAULT N'General';
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'ExtractedText') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [ExtractedText] nvarchar(max) NULL;
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'AiSummary') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [AiSummary] nvarchar(max) NULL;
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'SummarizedAtUtc') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [SummarizedAtUtc] datetime2 NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CourseMaterials_CourseId_Category'
      AND [object_id] = OBJECT_ID(N'[nexus].[CourseMaterials]'))
BEGIN
    CREATE INDEX [IX_CourseMaterials_CourseId_Category]
    ON [nexus].[CourseMaterials] ([CourseId], [Category]);
END";

        await _context.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task PopulateFiltersAsync(DocumentSearchViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var courses = _context.Courses.AsNoTracking().AsQueryable();
        var materials = _context.CourseMaterials.AsNoTracking().AsQueryable();
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            courses = courses.Where(c => c.TeacherId == userId);
            materials = materials.Where(m => m.Course!.TeacherId == userId);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            courses = courses.Where(c => c.Enrollments.Any(e => e.StudentId == userId));
            materials = materials.Where(m => m.Course!.Enrollments.Any(e => e.StudentId == userId));
        }

        model.Courses = await courses.OrderBy(c => c.Name).Select(c => new SelectListItem($"{c.Name} ({c.Semester})", c.Id.ToString())).ToListAsync();
        model.Categories = await materials.Select(m => m.Category).Distinct().OrderBy(c => c).Select(c => new SelectListItem(c, c)).ToListAsync();
    }
}
