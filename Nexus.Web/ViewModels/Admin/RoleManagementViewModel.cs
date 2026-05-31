using Nexus.Data.Identity;

namespace Nexus.Web.ViewModels.Admin;

public class RoleManagementViewModel
{
    public IReadOnlyList<UserRoleViewModel> Users { get; set; } = [];
    public IReadOnlyList<string> Roles { get; set; } = ApplicationRoles.All;
}
