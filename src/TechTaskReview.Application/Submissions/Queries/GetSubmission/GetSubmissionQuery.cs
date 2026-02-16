using MediatR;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmission;

public record GetSubmissionQuery(Guid SubmissionId) : IRequest<SubmissionDetailDto?>;
