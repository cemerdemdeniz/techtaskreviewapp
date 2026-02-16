using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;

namespace TechTaskReview.Application.Candidates.Queries.GetCandidates;

public class GetCandidatesQueryHandler : IRequestHandler<GetCandidatesQuery, PaginatedList<CandidateListDto>>
{
    private readonly ICandidateRepository _candidates;

    public GetCandidatesQueryHandler(ICandidateRepository candidates)
    {
        _candidates = candidates;
    }

    public async Task<PaginatedList<CandidateListDto>> Handle(GetCandidatesQuery request, CancellationToken ct)
    {
        var result = await _candidates.GetPaginatedAsync(
            request.Page, request.PageSize, request.Role, request.Search, ct);

        var dtos = result.Items.Select(c => new CandidateListDto(
            c.Id,
            c.FullName,
            c.Email,
            c.Role.ToString(),
            c.Position,
            c.Submissions.Count,
            c.Submissions
                .Where(s => s.Review is not null)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault()?.Review?.WeightedTotalScore,
            c.CreatedAt
        )).ToList();

        return new PaginatedList<CandidateListDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }
}
