using MediatR;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmissionStatus;

public record GetSubmissionStatusQuery(Guid SubmissionId) : IRequest<SubmissionStatusDto?>;

public record SubmissionStatusDto(
    Guid Id,
    string Status,
    string? FailureReason,
    DateTime? ProcessingStartedAt,
    DateTime? ProcessingCompletedAt,
    Guid? ReviewId);
