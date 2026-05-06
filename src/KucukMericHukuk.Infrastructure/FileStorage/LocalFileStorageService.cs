using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            content.Position = 0;
            await content.CopyToAsync(fileStream, ct);
        }

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Dosya silinirken hata: {Path}", fullPath);
            }
        }
        return Task.CompletedTask;
    }

    public bool Exists(string relativePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);
        return File.Exists(fullPath);
    }

    public string GetPublicUrl(string relativePath)
    {
        return "/" + relativePath.Replace('\\', '/').TrimStart('/');
    }
}
