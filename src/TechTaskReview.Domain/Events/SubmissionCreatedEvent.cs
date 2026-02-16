using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Events;

public record SubmissionCreatedEvent(Guid SubmissionId, Guid CandidateId, CandidateRole Role) : IDomainEvent;
