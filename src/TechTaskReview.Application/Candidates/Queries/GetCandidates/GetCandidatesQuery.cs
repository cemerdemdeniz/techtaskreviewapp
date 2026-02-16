using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Application.Candidates.Queries.GetCandidates;

public record GetCandidatesQuery(int Page = 1, int PageSize = 20, CandidateRole? Role = null, string? Search = null)
    : IRequest<PaginatedList<CandidateListDto>>;

public record CandidateListDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? Position,
    int SubmissionCount,
    decimal? LatestScore,
    DateTime CreatedAt);
