# 12 — Failure Recovery Design

## Retry Policies

### Per-Component Retry Strategy

| Component | Retry Count | Backoff | Max Delay | Circuit Breaker |
|-----------|------------|---------|-----------|-----------------|
| Ollama API | 3 | Exponential (2^n seconds) | 60s | Yes (5 failures → 1 min break) |
| OpenAI API | 3 | Exponential + jitter | 120s | Yes (5 failures → 2 min break) |
| ClamAV | 3 | Linear (10s) | 30s | Yes (3 failures → 5 min break) |
| MinIO/S3 | 3 | Exponential (2^n seconds) | 30s | No (essential service) |
| PostgreSQL | 2 | Immediate retry once, then 5s | 5s | No |
| Redis | 2 | 1s, 3s | 3s | No (degrade gracefully) |
| Static analysis (Docker) | 1 | N/A | N/A | No (non-fatal) |

### Hangfire Job Retry

```csharp
// Global default
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
{
    Attempts = 3,
    DelaysInSeconds = [30, 120, 600],
    OnAttemptsExceeded = AttemptsExceededAction.Fail
});

// Per-job override for AI review (longer timeouts expected)
[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900, 1800, 3600 })]
public class AIReviewJob { ... }

// Non-retryable jobs (cleanup is idempotent, will run again tomorrow)
[AutomaticRetry(Attempts = 0)]
public class CleanupJob { ... }
```

## Circuit Breaker Configuration

```
                    ┌─────────┐
              ┌────▶│ CLOSED  │◀────────────────────┐
              │     └────┬────┘                      │
              │          │                           │
              │     5 failures                  Success threshold
              │     in window                   met (3 successes)
              │          │                           │
              │          ▼                           │
              │     ┌─────────┐                 ┌────┴─────┐
              │     │  OPEN   │────────────────▶│HALF-OPEN │
              │     └─────────┘   Break expires │          │
              │                   (1-5 min)     └──────────┘
              │                                      │
              └──────────────────────────────────────┘
                         Failure in half-open
```

```csharp
// Polly circuit breaker for AI providers
var circuitBreakerPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => (int)r.StatusCode >= 500)
    .AdvancedCircuitBreakerAsync(
        failureThreshold: 0.5,             // 50% failure rate
        samplingDuration: TimeSpan.FromSeconds(30),
        minimumThroughput: 5,              // Need at least 5 requests in window
        durationOfBreak: TimeSpan.FromMinutes(1),
        onBreak: (result, duration) =>
        {
            Log.Error("AI circuit breaker OPEN for {Duration}. Last error: {Error}",
                duration, result.Exception?.Message ?? result.Result.StatusCode.ToString());
        },
        onReset: () => Log.Information("AI circuit breaker CLOSED. Service recovered."),
        onHalfOpen: () => Log.Information("AI circuit breaker HALF-OPEN. Testing service...")
    );
```

## Dead Letter Queue

When a Hangfire job exhausts all retries:

```csharp
public class FailedJobHandler : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is FailedState failedState)
        {
            // Extract submission ID from job arguments
            var submissionId = ExtractSubmissionId(context.BackgroundJob.Job);

            if (submissionId.HasValue)
            {
                // Move to dead letter record
                var deadLetterService = context.ServiceProvider.GetRequiredService<IDeadLetterService>();
                deadLetterService.RecordFailure(new DeadLetterEntry
                {
                    SubmissionId = submissionId.Value,
                    JobId = context.BackgroundJob.Id,
                    JobType = context.BackgroundJob.Job.Type.Name,
                    FailureReason = failedState.Exception?.Message ?? "Unknown",
                    StackTrace = failedState.Exception?.StackTrace,
                    FailedAt = DateTime.UtcNow,
                    RetryCount = context.BackgroundJob.Job.Args.Count // approx
                });

                // Update submission status
                var submissions = context.ServiceProvider.GetRequiredService<ISubmissionRepository>();
                var unitOfWork = context.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var submission = submissions.GetByIdAsync(submissionId.Value, CancellationToken.None)
                    .GetAwaiter().GetResult();

                submission?.MarkFailed($"Processing failed after maximum retries: {failedState.Exception?.Message}");
                unitOfWork.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }
}
```

## Partial Failure Handling

### AI Review Partial Success

If some chunks fail but ≥60% succeed, we compute a partial score:

```csharp
public class ScoreAggregationJob
{
    public async Task ExecuteAsync(Guid submissionId, CancellationToken ct)
    {
        var review = await _reviews.GetBySubmissionIdAsync(submissionId, ct);
        var aggregatedResult = _aggregator.Aggregate(
            review.ChunkResults, review.ChunksFailed, review.ChunksProcessed);

        // Determine if we have enough data
        var successRate = (double)review.ChunksProcessed / (review.ChunksProcessed + review.ChunksFailed);

        if (successRate < 0.6)
        {
            // Too many failures — mark as Failed
            review.Fail(
                JsonSerializer.Serialize(aggregatedResult),
                review.ProcessingDuration);
        }
        else
        {
            // Enough data — compute partial score with penalty
            var scoringConfig = await _configs.GetDefaultForRoleAsync(review.Role, ct);
            var result = _scoreComputer.Compute(aggregatedResult.Categories, scoringConfig);

            // Apply partial data penalty: reduce score proportional to missing data
            var partialPenalty = successRate < 1.0m ? (decimal)successRate : 1.0m;
            var adjustedTotal = result.WeightedTotal * partialPenalty;

            review.Complete(
                adjustedTotal,
                aggregatedResult.Summary + $"\n\n⚠️ Partial review: {review.ChunksFailed} of {review.ChunksProcessed + review.ChunksFailed} chunks failed. Score adjusted by {partialPenalty:P0}.",
                JsonSerializer.Serialize(aggregatedResult),
                review.ProcessingDuration,
                review.ChunksProcessed,
                review.ChunksFailed);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

## Idempotency

All background jobs must be safe to re-execute:

| Job | Idempotency Strategy |
|-----|---------------------|
| FileProcessingJob | Check submission status before processing. If already past Extracting, skip. |
| StaticAnalysisJob | Overwrite previous results. Static analysis is deterministic. |
| AIReviewJob | Check if CodeReview exists for submission. If completed, skip. If in-progress, resume from last processed chunk. |
| ScoreAggregationJob | Recompute and overwrite. Scoring is deterministic given inputs. |
| PdfExportJob | Check if PDF exists in storage. If exists and not expired, skip. |
| CleanupJob | All operations are "delete if exists" — naturally idempotent. |

## Data Consistency

### Submission State Machine Enforcement

The domain model's `TransitionTo()` method prevents invalid state transitions. If a Hangfire job tries to transition a Quarantined submission to AIReview, a `DomainException` is thrown and the job fails cleanly.

### Distributed Locking

For operations that must be exclusive per submission (e.g., only one AI review should run at a time):

```csharp
public class AIReviewJob
{
    private readonly IDistributedLockProvider _lockProvider;

    public async Task ExecuteAsync(Guid submissionId, CancellationToken ct)
    {
        var lockKey = $"ai-review:{submissionId}";

        await using var lockHandle = await _lockProvider.TryAcquireAsync(
            lockKey,
            timeout: TimeSpan.FromSeconds(10),
            ct);

        if (lockHandle is null)
        {
            // Another worker is already processing this submission
            // Hangfire will retry later
            throw new InvalidOperationException(
                $"Could not acquire lock for AI review of submission {submissionId}. Another worker is processing it.");
        }

        // Proceed with review...
    }
}
```

## Disaster Recovery

| Scenario | Impact | Recovery |
|----------|--------|---------|
| PostgreSQL crash | Data loss risk | Automated daily backups (pg_dump). WAL archiving for point-in-time recovery. RPO: 5 minutes. |
| Redis crash | Queue state lost | Hangfire uses Redis persistence. Jobs in-flight will be re-enqueued on restart. Cache data is regenerated. |
| MinIO crash | File access lost | MinIO erasure coding (4 disks min). S3 versioning enabled. Daily cross-region backup for production. |
| Ollama crash | AI review stalls | Circuit breaker opens. Jobs queue up. Auto-recover when Ollama restarts. Fallback to cloud AI if configured. |
| Worker crash | In-flight jobs lost | Hangfire re-enqueues failed jobs. Idempotent design ensures safe re-execution. |
| Full host failure | Complete outage | Docker Compose: manual failover to standby. K8s: automatic pod rescheduling. |

## Monitoring & Alerting

| Metric | Source | Alert Condition |
|--------|--------|----------------|
| Queue depth | Hangfire API | > 3,000 for > 10 minutes |
| Failed jobs/hour | Hangfire dashboard | > 50/hour |
| Circuit breaker state | Application logs | Any circuit OPEN |
| Submission failure rate | PostgreSQL | > 5% of submissions in Failed state |
| AI review latency p95 | Application metrics | > 15 minutes |
| Disk usage (MinIO) | MinIO metrics | > 80% capacity |
| PostgreSQL connections | pg_stat_activity | > 80 of max_connections |
| ClamAV signature age | ClamAV logs | > 7 days since last update |
