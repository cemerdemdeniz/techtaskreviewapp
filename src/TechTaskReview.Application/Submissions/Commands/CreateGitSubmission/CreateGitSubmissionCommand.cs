using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Application.Submissions.Commands.CreateGitSubmission;

public record CreateGitSubmissionCommand(
    Guid CandidateId,
    string GitUrl,
    string? Branch,
    CandidateRole Role
) : IRequest<Result<Guid>>;
