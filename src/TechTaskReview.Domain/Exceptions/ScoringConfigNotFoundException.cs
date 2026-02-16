namespace TechTaskReview.Domain.Exceptions;

public class ScoringConfigNotFoundException : DomainException
{
    public ScoringConfigNotFoundException(string message) : base(message) { }
    public ScoringConfigNotFoundException(Guid configId)
        : base($"Scoring configuration with ID '{configId}' was not found.") { }
}
