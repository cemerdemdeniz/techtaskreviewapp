using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Application.Common.Interfaces;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Candidate?> GetByEmailAsync(string email, CancellationToken ct);
    Task<PaginatedList<Candidate>> GetPaginatedAsync(int page, int pageSize, CandidateRole? roleFilter, string? search, CancellationToken ct);
    void Add(Candidate candidate);
    void Update(Candidate candidate);
    void Delete(Candidate candidate);
}
