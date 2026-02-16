using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmissions;

public class GetSubmissionsQueryHandler : IRequestHandler<GetSubmissionsQuery, PaginatedList<SubmissionListDto>>
{
    private readonly ISubmissionRepository _submissions;

    public GetSubmissionsQueryHandler(ISubmissionRepository submissions)
    {
        _submissions = submissions;
    }

    public async Task<PaginatedList<SubmissionListDto>> Handle(GetSubmissionsQuery request, CancellationToken ct)
    {
        var result = await _submissions.GetPaginatedAsync(request.Page, request.PageSize, null, ct);

        var dtos = result.Items.Select(s => new SubmissionListDto(
            s.Id,
            s.CandidateId,
            string.Empty,
            s.Status.ToString(),
            s.OriginalFileName,
            s.Review?.WeightedTotalScore,
            s.CreatedAt
        )).ToList();

        return new PaginatedList<SubmissionListDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }
}
