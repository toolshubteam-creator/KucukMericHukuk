namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
    bool Exists(string relativePath);
    string GetPublicUrl(string relativePath);
}
