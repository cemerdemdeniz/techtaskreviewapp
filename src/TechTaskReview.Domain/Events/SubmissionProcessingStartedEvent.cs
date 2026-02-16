using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Events;

public record SubmissionProcessingStartedEvent(Guid SubmissionId) : IDomainEvent;
