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
        await BaselineExistingSchemaAsync(dbContext);
        await dbContext.Database.MigrateAsync();
        await EnsureCourseMaterialRepositoryColumnsAsync(dbContext);

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

    private static async Task EnsureCourseMaterialRepositoryColumnsAsync(ApplicationDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer() || !await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'[nexus].[CourseMaterials]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'Category') IS NULL
    BEGIN
        ALTER TABLE [nexus].[CourseMaterials]
        ADD [Category] nvarchar(80) NOT NULL
            CONSTRAINT [DF_CourseMaterials_Category] DEFAULT N'General';
    END;

    IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'ExtractedText') IS NULL
    BEGIN
        ALTER TABLE [nexus].[CourseMaterials] ADD [ExtractedText] nvarchar(max) NULL;
    END;

    IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'AiSummary') IS NULL
    BEGIN
        ALTER TABLE [nexus].[CourseMaterials] ADD [AiSummary] nvarchar(max) NULL;
    END;

    IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'SummarizedAtUtc') IS NULL
    BEGIN
        ALTER TABLE [nexus].[CourseMaterials] ADD [SummarizedAtUtc] datetime2 NULL;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_CourseMaterials_CourseId_Category'
          AND [object_id] = OBJECT_ID(N'[nexus].[CourseMaterials]'))
    BEGIN
        CREATE INDEX [IX_CourseMaterials_CourseId_Category]
        ON [nexus].[CourseMaterials] ([CourseId], [Category]);
    END;
END");
    }

    private static async Task BaselineExistingSchemaAsync(ApplicationDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer() || !await dbContext.Database.CanConnectAsync())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'[nexus].[AspNetUsers]', N'U') IS NOT NULL
BEGIN
    IF SCHEMA_ID(N'nexus') IS NULL
    BEGIN
        EXEC(N'CREATE SCHEMA [nexus]');
    END;

    IF OBJECT_ID(N'[nexus].[__EFMigrationsHistory]', N'U') IS NULL
    BEGIN
        CREATE TABLE [nexus].[__EFMigrationsHistory]
        (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM [nexus].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260531000000_AddIdentityAuthentication')
    BEGIN
        INSERT INTO [nexus].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260531000000_AddIdentityAuthentication', N'9.0.0');
    END;

    IF OBJECT_ID(N'[nexus].[Courses]', N'U') IS NOT NULL
       AND OBJECT_ID(N'[nexus].[Assignments]', N'U') IS NOT NULL
       AND OBJECT_ID(N'[nexus].[Submissions]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [nexus].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260531001000_AddAcademicModels')
    BEGIN
        INSERT INTO [nexus].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260531001000_AddAcademicModels', N'9.0.0');
    END;

    IF OBJECT_ID(N'[nexus].[CourseMaterials]', N'U') IS NOT NULL
       AND OBJECT_ID(N'[nexus].[Quizzes]', N'U') IS NOT NULL
       AND COL_LENGTH(N'[nexus].[Submissions]', N'Id') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [nexus].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260531002000_AddTeacherStudentGradeQuizModules')
    BEGIN
        INSERT INTO [nexus].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260531002000_AddTeacherStudentGradeQuizModules', N'9.0.0');
    END;

    IF OBJECT_ID(N'[nexus].[CourseMaterials]', N'U') IS NOT NULL
       AND COL_LENGTH(N'[nexus].[CourseMaterials]', N'Category') IS NOT NULL
       AND COL_LENGTH(N'[nexus].[CourseMaterials]', N'AiSummary') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [nexus].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260531130000_AddRepositoryAiAnalyticsFields')
    BEGIN
        INSERT INTO [nexus].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260531130000_AddRepositoryAiAnalyticsFields', N'9.0.0');
    END;
END");
    }
}
