using MediatR;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Application.Candidates.Queries.GetCandidate;

public class GetCandidateQueryHandler : IRequestHandler<GetCandidateQuery, CandidateDetailDto?>
{
    private readonly ICandidateRepository _candidates;

    public GetCandidateQueryHandler(ICandidateRepository candidates)
    {
        _candidates = candidates;
    }

    public async Task<CandidateDetailDto?> Handle(GetCandidateQuery request, CancellationToken ct)
    {
        var candidate = await _candidates.GetByIdAsync(request.CandidateId, ct);
        if (candidate is null) return null;

        return new CandidateDetailDto(
            candidate.Id,
            candidate.FullName,
            candidate.Email,
            candidate.Role.ToString(),
            candidate.Position,
            candidate.Notes,
            candidate.Submissions.Select(s => new CandidateSubmissionDto(
                s.Id, s.Status.ToString(), s.OriginalFileName,
                s.Review?.WeightedTotalScore, s.CreatedAt
            )).ToList(),
            candidate.CreatedAt);
    }
}
