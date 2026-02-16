using MediatR;

namespace TechTaskReview.Application.Candidates.Queries.GetCandidate;

public record GetCandidateQuery(Guid CandidateId) : IRequest<CandidateDetailDto?>;

public record CandidateDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? Position,
    string? Notes,
    List<CandidateSubmissionDto> Submissions,
    DateTime CreatedAt);

public record CandidateSubmissionDto(
    Guid Id,
    string Status,
    string OriginalFileName,
    decimal? Score,
    DateTime CreatedAt);
