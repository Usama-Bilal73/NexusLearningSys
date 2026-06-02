using Microsoft.AspNetCore.Identity;
using Nexus.Business.Services;
using Nexus.Data.DependencyInjection;
using Nexus.Data.Identity;
using Nexus.Data.Persistence;
using Nexus.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});
builder.Services.AddRazorPages();

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddScoped<Nexus.Web.Services.IFileStorageService, Nexus.Web.Services.FileStorageService>();
builder.Services.AddScoped<Nexus.Web.Services.IGradebookService, Nexus.Web.Services.GradebookService>();
builder.Services.AddScoped<Nexus.Web.Services.IPdfTextExtractor, Nexus.Web.Services.PdfTextExtractor>();
builder.Services.AddHttpClient<Nexus.Web.Services.IOpenAiLearningService, Nexus.Web.Services.OpenAiLearningService>();
builder.Services.AddSingleton<Nexus.Web.Services.IVectorDatabaseService, Nexus.Web.Services.InMemoryVectorDatabaseService>();
builder.Services.AddScoped<Nexus.Web.Services.ICourseRagService, Nexus.Web.Services.CourseRagService>();
builder.Services.AddScoped<Nexus.Web.Services.IPerformanceAnalyticsService, Nexus.Web.Services.PerformanceAnalyticsService>();
builder.Services.AddScoped<Nexus.Web.Services.IGpaCalculationService, Nexus.Web.Services.GpaCalculationService>();
builder.Services.AddScoped<Nexus.Web.Services.IReportService, Nexus.Web.Services.ReportService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Run seeding conditionally to avoid crashing the host on startup in environments where the
// database may be misaligned. Enable by setting "RunMigrationsOnStartup": true in configuration.
var config = app.Services.GetRequiredService<IConfiguration>();
var runMigrations = config.GetValue<bool>("RunMigrationsOnStartup");
if (runMigrations)
{
    try
    {
        await IdentitySeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "An error occurred while running IdentitySeeder. Startup will continue.");
        Console.WriteLine($"IdentitySeeder error: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
