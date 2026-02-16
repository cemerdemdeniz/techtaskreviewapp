using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Application.Common.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Submission>> GetByCandidateIdAsync(Guid candidateId, CancellationToken ct);
    Task<PaginatedList<Submission>> GetPaginatedAsync(int page, int pageSize, SubmissionStatus? statusFilter, CancellationToken ct);
    void Add(Submission submission);
    void Update(Submission submission);
}
