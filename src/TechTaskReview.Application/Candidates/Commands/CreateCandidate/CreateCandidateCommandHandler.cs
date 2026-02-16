using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Application.Candidates.Commands.CreateCandidate;

public class CreateCandidateCommandHandler : IRequestHandler<CreateCandidateCommand, Result<Guid>>
{
    private readonly ICandidateRepository _candidates;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCandidateCommandHandler(ICandidateRepository candidates, IUnitOfWork unitOfWork)
    {
        _candidates = candidates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCandidateCommand request, CancellationToken ct)
    {
        var existing = await _candidates.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return Result<Guid>.Failure("A candidate with this email already exists.");

        var candidate = Candidate.Create(request.FullName, request.Email, request.Role, request.Position);
        _candidates.Add(candidate);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(candidate.Id);
    }
}
