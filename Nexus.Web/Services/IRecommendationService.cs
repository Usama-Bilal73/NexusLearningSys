using Nexus.Web.ViewModels.Recommendations;

namespace Nexus.Web.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationViewModel>> GetRecommendationsAsync(string userId, int top = 10, CancellationToken cancellationToken = default);
}
