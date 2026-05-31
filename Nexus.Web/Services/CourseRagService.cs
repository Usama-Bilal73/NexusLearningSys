using Microsoft.EntityFrameworkCore;
using Nexus.Data.Persistence;

namespace Nexus.Web.Services;

public interface ICourseRagService
{
    Task<(IReadOnlyList<string> Chunks, IReadOnlyList<string> Sources)> RetrieveAsync(int courseId, string question, CancellationToken cancellationToken = default);
}

public class CourseRagService : ICourseRagService
{
    private readonly ApplicationDbContext _context;
    private readonly IVectorDatabaseService _vectorDatabase;

    public CourseRagService(ApplicationDbContext context, IVectorDatabaseService vectorDatabase)
    {
        _context = context;
        _vectorDatabase = vectorDatabase;
    }

    public async Task<(IReadOnlyList<string> Chunks, IReadOnlyList<string> Sources)> RetrieveAsync(int courseId, string question, CancellationToken cancellationToken = default)
    {
        var documents = await _context.CourseMaterials.AsNoTracking()
            .Where(m => m.CourseId == courseId && (m.MaterialType == Nexus.Data.Models.CourseMaterialType.Syllabus || m.MaterialType == Nexus.Data.Models.CourseMaterialType.LectureNotes))
            .Select(m => new { m.Title, m.OriginalFileName, m.ExtractedText, m.AiSummary })
            .ToListAsync(cancellationToken);

        var chunks = documents.SelectMany((document, documentIndex) => SplitChunks(document.ExtractedText ?? document.AiSummary ?? string.Empty)
            .Select((chunk, chunkIndex) =>
            (
                Id: $"{documentIndex}-{chunkIndex}",
                Source: $"{document.Title} ({document.OriginalFileName})",
                Text: chunk
            )));

        var ranked = _vectorDatabase.Search(question, chunks, 5);
        return (ranked.Select(item => item.Text).ToList(), ranked.Select(item => item.Source).Distinct().ToList());
    }

    private static IEnumerable<string> SplitChunks(string text)
    {
        const int chunkSize = 1800;
        for (var index = 0; index < text.Length; index += chunkSize)
        {
            yield return text.Substring(index, Math.Min(chunkSize, text.Length - index));
        }
    }
}
