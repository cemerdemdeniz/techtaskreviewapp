using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Application.Common.Interfaces;

public interface ICodeReviewRepository
{
    Task<CodeReview?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CodeReview?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct);
    void Add(CodeReview review);
    void Update(CodeReview review);
}
