using FluentValidation;

namespace TechTaskReview.Application.Submissions.Commands.CreateGitSubmission;

public class CreateGitSubmissionCommandValidator : AbstractValidator<CreateGitSubmissionCommand>
{
    public CreateGitSubmissionCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.GitUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("Git URL must be a valid HTTPS URL.");
        RuleFor(x => x.Role).IsInEnum();
    }
}
