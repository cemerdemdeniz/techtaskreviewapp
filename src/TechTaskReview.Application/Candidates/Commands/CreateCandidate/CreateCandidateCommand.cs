using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Application.Candidates.Commands.CreateCandidate;

public record CreateCandidateCommand(
    string FullName,
    string Email,
    CandidateRole Role,
    string? Position,
    string? Notes
) : IRequest<Result<Guid>>;
