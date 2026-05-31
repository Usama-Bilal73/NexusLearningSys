using UglyToad.PdfPig;

namespace Nexus.Web.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(string webRelativePath, CancellationToken cancellationToken = default);
}

public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly IWebHostEnvironment _environment;

    public PdfTextExtractor(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<string> ExtractTextAsync(string webRelativePath, CancellationToken cancellationToken = default)
    {
        var relative = webRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.WebRootPath, relative);
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(string.Empty);
        }

        using var document = PdfDocument.Open(absolutePath);
        var pages = document.GetPages().Select(page => page.Text);
        return Task.FromResult(string.Join(Environment.NewLine + Environment.NewLine, pages));
    }
}
