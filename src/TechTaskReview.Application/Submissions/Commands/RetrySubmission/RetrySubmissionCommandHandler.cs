using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Application.Submissions.Commands.RetrySubmission;

public class RetrySubmissionCommandHandler : IRequestHandler<RetrySubmissionCommand, Result<Guid>>
{
    private readonly ISubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;

    public RetrySubmissionCommandHandler(ISubmissionRepository submissions, IUnitOfWork unitOfWork)
    {
        _submissions = submissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RetrySubmissionCommand request, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null)
            return Result<Guid>.Failure("Submission not found.");

        submission.ResetForRetry();
        _submissions.Update(submission);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(submission.Id);
    }
}
