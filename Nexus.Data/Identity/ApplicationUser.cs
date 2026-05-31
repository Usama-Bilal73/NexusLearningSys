using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Nexus.Data.Identity;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
