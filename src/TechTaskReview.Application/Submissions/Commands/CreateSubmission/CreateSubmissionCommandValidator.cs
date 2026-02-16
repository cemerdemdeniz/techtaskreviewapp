using FluentValidation;

namespace TechTaskReview.Application.Submissions.Commands.CreateSubmission;

public class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    private static readonly string[] AllowedExtensions = [".zip", ".tar.gz", ".tgz"];
    private const long MaxFileSize = 100 * 1024 * 1024;

    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => AllowedExtensions.Any(ext => name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"File must be one of: {string.Join(", ", AllowedExtensions)}");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)}MB.");
        RuleFor(x => x.Role).IsInEnum();
    }
}
