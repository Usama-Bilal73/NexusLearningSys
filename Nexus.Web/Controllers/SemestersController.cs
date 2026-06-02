using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class SemestersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SemestersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var semesters = await _context.Semesters.AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
        return View(semesters);
    }

    public IActionResult Create() => View(new Semester { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(4) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Semester model)
    {
        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");
        if (!ModelState.IsValid) return View(model);

        model.CreatedAtUtc = DateTime.UtcNow;
        _context.Semesters.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Semester '{model.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester is null) return NotFound();
        return View(semester);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Semester model)
    {
        if (id != model.Id) return BadRequest();
        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");
        if (!ModelState.IsValid) return View(model);

        var semester = await _context.Semesters.FindAsync(id);
        if (semester is null) return NotFound();

        semester.Name        = model.Name.Trim();
        semester.Description = model.Description?.Trim();
        semester.StartDate   = model.StartDate;
        semester.EndDate     = model.EndDate;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Semester updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id)
    {
        // Deactivate all, then activate the selected one
        await _context.Semesters.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        var semester = await _context.Semesters.FindAsync(id);
        if (semester is null) return NotFound();
        semester.IsActive = true;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"'{semester.Name}' is now the active semester.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var semester = await _context.Semesters.Include(s => s.Courses).FirstOrDefaultAsync(s => s.Id == id);
        if (semester is null) return NotFound();
        if (semester.Courses.Count > 0)
        {
            TempData["Error"] = "Cannot delete a semester that has courses assigned to it.";
            return RedirectToAction(nameof(Index));
        }
        _context.Semesters.Remove(semester);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Semester deleted.";
        return RedirectToAction(nameof(Index));
    }
}
