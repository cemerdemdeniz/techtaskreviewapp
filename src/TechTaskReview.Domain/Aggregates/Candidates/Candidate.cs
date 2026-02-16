using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Domain.Common;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Domain.Aggregates.Candidates;

public class Candidate : AggregateRoot
{
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public CandidateRole Role { get; private set; }
    public string? Position { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<Submission> _submissions = [];
    public IReadOnlyList<Submission> Submissions => _submissions.AsReadOnly();

    private Candidate() { } // EF Core

    public static Candidate Create(string fullName, string email, CandidateRole role, string? position = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidSubmissionException("Full name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidSubmissionException("Email is required.");

        return new Candidate
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Position = position
        };
    }
}
