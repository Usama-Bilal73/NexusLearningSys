using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nexus.Data.Identity;
using Nexus.Web.Services;

namespace Nexus.Web.Controllers;

[Authorize]
public class RecommendationsController : Controller
{
    private readonly IRecommendationService _recommendations;
    private readonly UserManager<ApplicationUser> _userManager;

    public RecommendationsController(IRecommendationService recommendations, UserManager<ApplicationUser> userManager)
    {
        _recommendations = recommendations;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var recs = await _recommendations.GetRecommendationsAsync(userId, 10);
        return View(recs);
    }
}
