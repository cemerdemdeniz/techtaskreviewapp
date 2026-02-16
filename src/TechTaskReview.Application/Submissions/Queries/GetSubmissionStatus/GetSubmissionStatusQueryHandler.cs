using MediatR;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmissionStatus;

public class GetSubmissionStatusQueryHandler : IRequestHandler<GetSubmissionStatusQuery, SubmissionStatusDto?>
{
    private readonly ISubmissionRepository _submissions;

    public GetSubmissionStatusQueryHandler(ISubmissionRepository submissions)
    {
        _submissions = submissions;
    }

    public async Task<SubmissionStatusDto?> Handle(GetSubmissionStatusQuery request, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return null;

        return new SubmissionStatusDto(
            submission.Id,
            submission.Status.ToString(),
            submission.FailureReason,
            submission.ProcessingStartedAt,
            submission.ProcessingCompletedAt,
            submission.Review?.Id);
    }
}
