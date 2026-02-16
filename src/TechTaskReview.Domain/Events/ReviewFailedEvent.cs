using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Events;

public record ReviewFailedEvent(Guid SubmissionId, Guid CandidateId, string Reason) : IDomainEvent;
