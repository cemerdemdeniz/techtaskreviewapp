using MediatR;
using TechTaskReview.Application.Common.Models;

namespace TechTaskReview.Application.Submissions.Commands.RetrySubmission;

public record RetrySubmissionCommand(Guid SubmissionId) : IRequest<Result<Guid>>;
