namespace Nexus.Web.ViewModels.Analytics;

public record LabelValueDto(string Label, long Value);

public record CoursePopularityDto(string CourseName, long EnrollmentCount);
