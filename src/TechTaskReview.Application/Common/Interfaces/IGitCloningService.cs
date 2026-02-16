namespace TechTaskReview.Application.Common.Interfaces;

public interface IGitCloningService
{
    Task<string> CloneAsync(string gitUrl, string? branch, CancellationToken ct);
}
