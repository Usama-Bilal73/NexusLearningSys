using Microsoft.AspNetCore.Hosting;

namespace Nexus.Web.Services;

public interface IFileStorageService
{
    Task<string> SaveUploadAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
}

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".zip", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSize = 25 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveUploadAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Select a non-empty file.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Files cannot exceed 25 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        var safeFolder = string.Join('-', folderName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var relativeFolder = Path.Combine("uploads", safeFolder, DateTime.UtcNow.ToString("yyyyMMdd"));
        var absoluteFolder = Path.Combine(_environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using var stream = File.Create(absolutePath);
        await file.CopyToAsync(stream, cancellationToken);

        return "/" + Path.Combine(relativeFolder, fileName).Replace(Path.DirectorySeparatorChar, '/');
    }
}
