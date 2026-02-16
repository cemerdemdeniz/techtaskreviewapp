using MediatR;
using Microsoft.Extensions.Logging;
using TechTaskReview.Domain.Events;

namespace TechTaskReview.Application.Submissions.EventHandlers;

public class ReviewCompletedEventHandler : INotificationHandler<ReviewCompletedEvent>
{
    private readonly ILogger<ReviewCompletedEventHandler> _logger;

    public ReviewCompletedEventHandler(ILogger<ReviewCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ReviewCompletedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Review completed for submission {SubmissionId}, candidate {CandidateId}",
            notification.SubmissionId, notification.CandidateId);
        // Future: send email notification
        return Task.CompletedTask;
    }
}
