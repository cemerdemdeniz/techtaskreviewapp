namespace TechTaskReview.Domain.Aggregates.Reviews;

public enum ReviewStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Partial = 4     // Some chunks failed but enough data for scoring
}
