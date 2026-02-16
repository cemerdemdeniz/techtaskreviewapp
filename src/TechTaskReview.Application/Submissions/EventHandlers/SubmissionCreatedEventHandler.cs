using Hangfire;
using MediatR;
using TechTaskReview.Domain.Events;

namespace TechTaskReview.Application.Submissions.EventHandlers;

public class SubmissionCreatedEventHandler : INotificationHandler<SubmissionCreatedEvent>
{
    private readonly IBackgroundJobClient _jobClient;

    public SubmissionCreatedEventHandler(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    public Task Handle(SubmissionCreatedEvent notification, CancellationToken ct)
    {
        // Enqueue background processing job
        _jobClient.Enqueue<IFileProcessingJob>(
            job => job.ExecuteAsync(notification.SubmissionId, CancellationToken.None));
        return Task.CompletedTask;
    }
}

public interface IFileProcessingJob
{
    Task ExecuteAsync(Guid submissionId, CancellationToken ct);
}
