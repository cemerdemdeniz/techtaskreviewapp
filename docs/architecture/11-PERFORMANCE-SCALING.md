# 11 — Performance & Scaling

## Load Profile

```
Monthly submissions:        100,000
Daily average:              ~3,333
Hourly average:             ~139
Peak hour multiplier:       5x → ~700/hour
Peak concurrent uploads:    500
Average repo size:          25MB (compressed), ~75MB (extracted)
Average files per repo:     200
Average LOC per repo:       8,000
```

## Storage Budget

| Asset | Per Submission | Monthly (100k) | Retention | Effective Monthly |
|-------|---------------|-----------------|-----------|-------------------|
| Raw upload (.zip) | 25 MB | 2.5 TB | 2 years | 2.5 TB (cumulative) |
| Extracted repo | 75 MB | 7.5 TB | 30 days | 7.5 TB |
| AI responses (JSONB) | ~50 KB | 5 GB | 1 year | 5 GB |
| Generated PDFs | ~500 KB | 50 GB | 90 days | 50 GB |
| **Total active storage** | | | | **~10 TB** |

**MinIO/S3 cost** at S3 standard pricing: ~$230/month for 10TB. With MinIO self-hosted: hardware cost only.

## Queue Strategy

### Hangfire Queue Design

```
┌──────────────────────────────────────────────────────────┐
│                    Queue Architecture                     │
│                                                           │
│  ┌─────────────┐  Priority: Highest                      │
│  │  critical    │  - Malware scan results                 │
│  │  (1 worker)  │  - Quarantine actions                   │
│  └─────────────┘                                          │
│                                                           │
│  ┌─────────────┐  Priority: High                          │
│  │  default     │  - File extraction                      │
│  │  (4 workers) │  - Static analysis                      │
│  │              │  - Score computation                     │
│  └─────────────┘                                          │
│                                                           │
│  ┌─────────────┐  Priority: Normal                        │
│  │  ai-review   │  - AI review chunks                     │
│  │  (8 workers) │  - Rate-limit aware                     │
│  └─────────────┘                                          │
│                                                           │
│  ┌─────────────┐  Priority: Low                           │
│  │  export      │  - PDF generation                       │
│  │  (2 workers) │  - Cleanup tasks                        │
│  └─────────────┘                                          │
└──────────────────────────────────────────────────────────┘
```

### Worker Count Rationale

The `ai-review` queue gets the most workers because AI inference is the bottleneck. Each worker sends a request to Ollama and waits for the response (I/O bound). With 8 workers and ~30s per chunk inference:

- **Throughput**: 8 chunks / 30s = 16 chunks/minute
- **Per submission**: 20 chunks → ~1.25 minutes for AI review
- **Hourly capacity**: ~48 submissions/hour (AI stage only)
- **For 700/hour peak**: Need ~15 Ollama instances or faster model

**Scaling decision**: At peak load, either:
1. Accept longer queue times (submissions complete in ~45 min instead of ~8 min)
2. Scale Ollama instances horizontally
3. Overflow to cloud AI (GPT-4o-mini at $0.02/submission — $14/hr at peak)

### Queue Backpressure

```csharp
// Reject new submissions if queue depth exceeds threshold
public class BackpressureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBackgroundJobClient _hangfire;
    private const int MaxQueueDepth = 5000;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/submissions") &&
            context.Request.Method == "POST")
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var enqueuedCount = monitoringApi.EnqueuedCount("default")
                              + monitoringApi.EnqueuedCount("ai-review");

            if (enqueuedCount > MaxQueueDepth)
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    errors = new[] { new
                    {
                        code = "SERVICE_BUSY",
                        message = "System is processing a high volume of submissions. Please retry in a few minutes."
                    }}
                });
                return;
            }
        }

        await _next(context);
    }
}
```

## Parallelism Model

```
Single Submission Pipeline:
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  ┌──────────┐
│ Extract  │─▶│ Scan     │─▶│ Static   │─▶│ AI Review        │─▶│ Score    │
│ (serial) │  │ (||10)   │  │ Analysis │  │ (|| concurrency) │  │ (serial) │
│ ~30s     │  │ ~60s     │  │ (serial) │  │ ~2-5 min         │  │ ~1s      │
└──────────┘  └──────────┘  │ ~60s     │  └──────────────────┘  └──────────┘
                             └──────────┘

|| = parallel within stage
```

- **Extraction**: Serial (single archive)
- **Malware scan**: Parallel 10 files at a time (SemaphoreSlim)
- **Static analysis**: Serial (one analyzer at a time — each runs in its own Docker container)
- **AI review**: Parallel chunks, concurrency limited by provider rate limits
- **Scoring**: Serial (fast computation, <1s)

## Caching Strategy

### Redis Cache Layers

| Cache Key Pattern | TTL | Purpose |
|------------------|-----|---------|
| `dashboard:summary` | 60s | Dashboard metrics (expensive aggregation query) |
| `rankings:{role}:{configId}:page:{n}` | 120s | Ranking queries (common access pattern) |
| `review:{id}` | 300s | Review detail (immutable after completion) |
| `scoring-config:{id}` | 600s | Scoring config (rarely changes) |
| `candidate:{id}` | 120s | Candidate detail |
| `submission:status:{id}` | 5s | Polling endpoint (very frequent, short TTL) |

### Cache Invalidation

- **Write-through** for scoring configs: update cache on mutation
- **TTL-based** for dashboard, rankings: acceptable staleness
- **Event-driven** for review completion: invalidate rankings cache when new review completes

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public async Task<T?> GetOrSetAsync<T>(
        string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached);

        var value = await factory();
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);

        return value;
    }

    public async Task InvalidateAsync(string keyPattern, CancellationToken ct)
    {
        // For pattern-based invalidation (e.g., all ranking pages)
        // Use Redis SCAN + DEL (not KEYS — KEYS blocks)
        // Implementation depends on StackExchange.Redis multiplexer
    }
}
```

## Horizontal Scaling

### MVP (Single Host)

```
docker-compose.yml:
  1x API, 1x Worker, 1x PostgreSQL, 1x Redis, 1x MinIO, 1x Ollama, 1x ClamAV
  Requirement: 32GB RAM, 8 cores, 1x GPU (RTX 3060+ for Ollama)
```

### Scale-Out Phase 1 (3 hosts)

```
Host 1 (API): 3x API instances behind Nginx, 1x Redis
Host 2 (Workers): 2x Worker instances, 2x Ollama (GPU)
Host 3 (Data): PostgreSQL primary, MinIO, ClamAV
```

### Scale-Out Phase 2 (Kubernetes)

```
API: HPA 3-10 pods (CPU target: 60%)
Workers: HPA 2-8 pods (queue depth target)
Ollama: StatefulSet 2-4 pods (GPU nodes)
PostgreSQL: Managed service (RDS/CloudNative-PG)
Redis: Managed service or Redis Sentinel
MinIO: Managed S3 or MinIO Operator
```

## Cost Control

### AI Cost Budget

```
Monthly budget: $500 (for hybrid approach)

Strategy:
- Ollama handles 90% of submissions (free, local)
- Overflow 10% to GPT-4o-mini at peak ($0.02/submission)
- Per-month: 90,000 × $0 + 10,000 × $0.02 = $200

Monitoring:
- Alert at $400/month
- Hard cap at $500/month (reject overflow to cloud)
- Dashboard shows daily AI cost trend
```

### Storage Cost Control

```
Aggressive cleanup policy:
- Extracted repos: 30 days → saves ~7.5 TB/month
- PDFs: 90 days → saves ~150 GB/quarter
- Raw uploads archived to cold storage after 6 months

MinIO lifecycle rules handle this automatically.
```

## Performance Targets & SLOs

| Metric | Target | Alert Threshold |
|--------|--------|----------------|
| API p50 latency | < 100ms | > 200ms |
| API p99 latency | < 500ms | > 1s |
| Upload accept latency | < 2s | > 5s |
| Pipeline p50 e2e | < 5 min | > 10 min |
| Pipeline p95 e2e | < 15 min | > 25 min |
| Queue depth (default) | < 500 | > 2,000 |
| Queue depth (ai-review) | < 1,000 | > 3,000 |
| AI provider availability | > 99% | < 95% |
| Error rate (5xx) | < 0.1% | > 1% |

## Database Performance

### Query Optimization

Most frequent queries and their optimization:

| Query | Frequency | Index | Expected Latency |
|-------|-----------|-------|-----------------|
| Get submission by ID | Very high | PK | < 1ms |
| List submissions paginated | High | `ix_submissions_status_created` | < 10ms |
| Get review + category scores | High | PK + FK index | < 5ms |
| Dashboard aggregation | Medium | Multiple (cached 60s) | < 50ms (cache hit) |
| Candidate search by name | Medium | `ix_candidates_name_trgm` | < 20ms |
| Rankings by score | Medium | `ix_code_reviews_score` (cached) | < 10ms |

### Connection Pooling

```csharp
// EF Core connection pool
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.MinBatchSize(2);
    npgsqlOptions.MaxBatchSize(100);
});

// Npgsql connection pool (configured in connection string)
// "Host=pg;Database=techtask;Minimum Pool Size=10;Maximum Pool Size=100;"
```

At 100k submissions/month, write load is ~4 writes/second average (submission + files + review + scores). PostgreSQL handles this trivially. Read load (dashboard, listings) is cached.
