using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Business.Interfaces;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Web.ViewModels.Admin;

namespace Nexus.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class CoursesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoursesController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        IQueryable<Course> query = _unitOfWork.Repository<Course>().Query()
            .AsNoTracking()
            .Include(course => course.Teacher)
            .Include(course => course.Department);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(course => course.Name.Contains(searchTerm) || course.Semester.Contains(searchTerm) || (course.Department != null && course.Department.Name.Contains(searchTerm)));
        }

        var total = await query.CountAsync();
        var courses = await query.OrderBy(course => course.Name)
            .Skip((Math.Max(pageNumber, 1) - 1) * pageSize)
            .Take(pageSize)
            .Select(course => new CourseListItemViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Semester = course.Semester,
                TeacherName = course.Teacher == null ? "Unassigned" : (course.Teacher.DisplayName ?? course.Teacher.Email ?? course.Teacher.UserName ?? "Teacher"),
                DepartmentName = course.Department == null ? "Unassigned" : course.Department.Name
            })
            .ToListAsync();

        return View(new PagedResult<CourseListItemViewModel>
        {
            Items = courses,
            SearchTerm = searchTerm,
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = pageSize,
            TotalItems = total
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _unitOfWork.Repository<Course>().Query()
            .AsNoTracking()
            .Include(item => item.Teacher)
            .Include(item => item.Department)
            .Include(item => item.Assignments)
            .FirstOrDefaultAsync(item => item.Id == id);
        return course is null ? NotFound() : View(course);
    }

    public async Task<IActionResult> Create() => View(await PopulateSelectionsAsync(new CourseFormViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        if (!await IsTeacherAsync(model.TeacherId)) ModelState.AddModelError(nameof(model.TeacherId), "Select a valid teacher.");
        if (!ModelState.IsValid) return View(await PopulateSelectionsAsync(model));
        await _unitOfWork.Repository<Course>().AddAsync(new Course
        {
            Name = model.Name.Trim(),
            Semester = model.Semester.Trim(),
            TeacherId = model.TeacherId,
            DepartmentId = model.DepartmentId
        });
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Course created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        return course is null ? NotFound() : View(await PopulateSelectionsAsync(CourseFormViewModel.FromEntity(course)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!await IsTeacherAsync(model.TeacherId)) ModelState.AddModelError(nameof(model.TeacherId), "Select a valid teacher.");
        if (!ModelState.IsValid) return View(await PopulateSelectionsAsync(model));
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course is null) return NotFound();
        course.Name = model.Name.Trim();
        course.Semester = model.Semester.Trim();
        course.TeacherId = model.TeacherId;
        course.DepartmentId = model.DepartmentId;
        _unitOfWork.Repository<Course>().Update(course);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Course updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var course = await _unitOfWork.Repository<Course>().Query().AsNoTracking().Include(item => item.Department).FirstOrDefaultAsync(item => item.Id == id);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course is null) return NotFound();
        _unitOfWork.Repository<Course>().Remove(course);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Course deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CourseFormViewModel> PopulateSelectionsAsync(CourseFormViewModel model)
    {
        var teachers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Teacher);
        model.Teachers = teachers.OrderBy(user => user.FullName).Select(user => new SelectListItem(user.FullName == string.Empty ? user.Email : user.FullName, user.Id));
        var departments = await _unitOfWork.Repository<Department>().Query().AsNoTracking().OrderBy(department => department.Name).ToListAsync();
        model.Departments = departments.Select(department => new SelectListItem(department.Name, department.Id.ToString()));
        return model;
    }

    private async Task<bool> IsTeacherAsync(string teacherId)
    {
        var teacher = await _userManager.FindByIdAsync(teacherId);
        return teacher is not null && await _userManager.IsInRoleAsync(teacher, ApplicationRoles.Teacher);
    }
}
