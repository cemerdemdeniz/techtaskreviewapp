using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Events;

public record ReviewCompletedEvent(Guid SubmissionId, Guid CandidateId) : IDomainEvent;
