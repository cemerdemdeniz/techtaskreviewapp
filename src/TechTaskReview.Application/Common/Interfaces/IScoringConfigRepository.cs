using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.ScoringConfigs;

namespace TechTaskReview.Application.Common.Interfaces;

public interface IScoringConfigRepository
{
    Task<ScoringConfig?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ScoringConfig?> GetDefaultForRoleAsync(CandidateRole role, CancellationToken ct);
    Task<IReadOnlyList<ScoringConfig>> GetAllAsync(CancellationToken ct);
    void Add(ScoringConfig config);
    void Update(ScoringConfig config);
}
