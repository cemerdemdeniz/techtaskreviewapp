using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Application.Submissions.Commands.CreateGitSubmission;

public class CreateGitSubmissionCommandHandler : IRequestHandler<CreateGitSubmissionCommand, Result<Guid>>
{
    private readonly ISubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGitSubmissionCommandHandler(ISubmissionRepository submissions, IUnitOfWork unitOfWork)
    {
        _submissions = submissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateGitSubmissionCommand request, CancellationToken ct)
    {
        var submission = Submission.CreateFromGit(
            request.CandidateId, request.GitUrl, request.Branch, request.Role);

        _submissions.Add(submission);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(submission.Id);
    }
}
