using Microsoft.AspNetCore.Http;

namespace Nexus.Web.ViewModels.Enrollments;

public class BulkEnrollmentViewModel
{
    public int CourseId { get; set; }
    public IFormFile? File { get; set; }
}
