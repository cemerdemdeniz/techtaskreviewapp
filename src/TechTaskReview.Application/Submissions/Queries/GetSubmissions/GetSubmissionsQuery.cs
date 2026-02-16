using MediatR;
using TechTaskReview.Application.Common.Models;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmissions;

public record GetSubmissionsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedList<SubmissionListDto>>;

public record SubmissionListDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string Status,
    string OriginalFileName,
    decimal? Score,
    DateTime CreatedAt);
