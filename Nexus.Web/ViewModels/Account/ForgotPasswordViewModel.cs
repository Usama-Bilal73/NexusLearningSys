using System.ComponentModel.DataAnnotations;

namespace Nexus.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
