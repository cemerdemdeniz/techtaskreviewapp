namespace TechTaskReview.Domain.Exceptions;

public class InvalidSubmissionException : DomainException
{
    public InvalidSubmissionException(string message) : base(message) { }
    public InvalidSubmissionException(string message, Exception innerException) : base(message, innerException) { }
}
