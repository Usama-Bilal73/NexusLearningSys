using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Web.ViewModels.Admin;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class DepartmentsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        var query = _unitOfWork.Repository<Department>().Query().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(department => department.Name.Contains(searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query.OrderBy(department => department.Name)
            .Skip((Math.Max(pageNumber, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new PagedResult<Department>
        {
            Items = items,
            SearchTerm = searchTerm,
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = pageSize,
            TotalItems = total
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var department = await _unitOfWork.Repository<Department>().Query()
            .AsNoTracking()
            .Include(item => item.Courses)
            .FirstOrDefaultAsync(item => item.Id == id);
        return department is null ? NotFound() : View(department);
    }

    public IActionResult Create() => View(new DepartmentFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        await _unitOfWork.Repository<Department>().AddAsync(new Department { Name = model.Name.Trim() });
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Department created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        return department is null ? NotFound() : View(DepartmentFormViewModel.FromEntity(department));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department is null) return NotFound();
        department.Name = model.Name.Trim();
        _unitOfWork.Repository<Department>().Update(department);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Department updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var department = await _unitOfWork.Repository<Department>().Query().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return department is null ? NotFound() : View(department);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department is null) return NotFound();
        _unitOfWork.Repository<Department>().Remove(department);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Department deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
