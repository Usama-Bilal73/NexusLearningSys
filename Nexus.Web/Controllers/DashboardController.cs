using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Data.Identity;

namespace Nexus.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(ApplicationRoles.Admin))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            return RedirectToAction(nameof(Teacher));
        }

        return RedirectToAction(nameof(Student));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    public IActionResult Admin()
    {
        return View();
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public IActionResult Teacher()
    {
        return RedirectToAction("Index", "TeacherDashboard");
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    public IActionResult Student()
    {
        return RedirectToAction("Index", "StudentDashboard");
    }
}
