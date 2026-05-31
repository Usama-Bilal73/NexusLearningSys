using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;

namespace Nexus.Data.Seed;

public static class IdentitySeeder
{
    private const string DefaultAdminEmail = "admin@nexus.local";
    private const string DefaultAdminPassword = "Admin@12345";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(error => error.Description)));
                }
            }
        }

        var adminEmail = configuration["DefaultAdmin:Email"] ?? DefaultAdminEmail;
        var adminPassword = configuration["DefaultAdmin:Password"] ?? DefaultAdminPassword;
        var adminFirstName = configuration["DefaultAdmin:FirstName"] ?? "System";
        var adminLastName = configuration["DefaultAdmin:LastName"] ?? "Administrator";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = adminFirstName,
                LastName = adminLastName,
                DisplayName = $"{adminFirstName} {adminLastName}",
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create default admin account {Email}: {Errors}", adminEmail, string.Join(", ", createResult.Errors.Select(error => error.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to add default admin account {Email} to Admin role: {Errors}", adminEmail, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
