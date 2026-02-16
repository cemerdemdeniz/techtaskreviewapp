using FluentValidation;

namespace TechTaskReview.Application.Candidates.Commands.CreateCandidate;

public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Position).MaximumLength(200);
    }
}
