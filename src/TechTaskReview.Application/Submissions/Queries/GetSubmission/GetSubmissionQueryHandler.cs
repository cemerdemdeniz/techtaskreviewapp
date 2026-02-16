using MediatR;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Application.Submissions.Queries.GetSubmission;

public class GetSubmissionQueryHandler : IRequestHandler<GetSubmissionQuery, SubmissionDetailDto?>
{
    private readonly ISubmissionRepository _submissions;
    private readonly ICandidateRepository _candidates;

    public GetSubmissionQueryHandler(ISubmissionRepository submissions, ICandidateRepository candidates)
    {
        _submissions = submissions;
        _candidates = candidates;
    }

    public async Task<SubmissionDetailDto?> Handle(GetSubmissionQuery request, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null) return null;

        var candidate = await _candidates.GetByIdAsync(submission.CandidateId, ct);

        return new SubmissionDetailDto(
            submission.Id,
            submission.CandidateId,
            candidate?.FullName ?? "Unknown",
            submission.Source.ToString(),
            submission.OriginalFileName,
            submission.FileSizeBytes,
            submission.Status.ToString(),
            submission.FailureReason,
            submission.DetectedProjectType is not null
                ? new ProjectTypeDto(
                    submission.DetectedProjectType.PrimaryLanguage,
                    submission.DetectedProjectType.Framework,
                    submission.DetectedProjectType.BuildFiles,
                    submission.DetectedProjectType.IsMonorepo)
                : null,
            submission.TotalFiles,
            submission.TotalLinesOfCode,
            submission.ProcessingStartedAt,
            submission.ProcessingCompletedAt,
            submission.Review?.Id,
            submission.CreatedAt);
    }
}
