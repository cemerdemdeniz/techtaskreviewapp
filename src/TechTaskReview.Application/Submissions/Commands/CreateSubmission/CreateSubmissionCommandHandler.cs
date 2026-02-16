using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Application.Submissions.Commands.CreateSubmission;

public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, Result<Guid>>
{
    private readonly ISubmissionRepository _submissions;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSubmissionCommandHandler(
        ISubmissionRepository submissions,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork)
    {
        _submissions = submissions;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSubmissionCommand request, CancellationToken ct)
    {
        var storagePath = await _fileStorage.StoreAsync(request.FileStream, request.FileName, ct);

        var submission = Submission.CreateFromUpload(
            request.CandidateId,
            storagePath,
            request.FileName,
            request.FileSize,
            request.Role);

        _submissions.Add(submission);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(submission.Id);
    }
}
