using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Application.Submissions.Commands.CreateSubmission;

public record CreateSubmissionCommand(
    Guid CandidateId,
    Stream FileStream,
    string FileName,
    long FileSize,
    CandidateRole Role
) : IRequest<Result<Guid>>;
