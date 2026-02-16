namespace TechTaskReview.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct);
    Task<Stream> RetrieveAsync(string storagePath, CancellationToken ct);
    Task DeleteAsync(string storagePath, CancellationToken ct);
    Task<bool> ExistsAsync(string storagePath, CancellationToken ct);
}
