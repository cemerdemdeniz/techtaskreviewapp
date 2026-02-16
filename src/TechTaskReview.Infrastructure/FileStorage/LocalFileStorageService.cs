using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath = "/tmp/techtask-storage")
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyy/MM/dd"), Guid.NewGuid().ToString(), fileName);
        var fullPath = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);
        return relativePath;
    }

    public Task<Stream> RetrieveAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct)
    {
        return Task.FromResult(File.Exists(Path.Combine(_basePath, storagePath)));
    }
}
