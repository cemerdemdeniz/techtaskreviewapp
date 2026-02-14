# 02 — Backend Architecture (Clean Architecture)

## Layer Dependency Rule

```
  Web (ASP.NET Core)
    │
    ▼
  Application (Use Cases, CQRS)
    │
    ▼
  Domain (Entities, Value Objects, Domain Events)
    ▲
    │
  Infrastructure (EF Core, Redis, Ollama, File Storage, ClamAV)
```

**Dependency flows inward.** Infrastructure depends on Application abstractions. Domain has zero external dependencies.

## Project Structure

```
src/
├── TechTaskReview.Domain/
│   ├── Aggregates/
│   │   ├── Submissions/
│   │   │   ├── Submission.cs                 # Aggregate root
│   │   │   ├── SubmissionFile.cs             # Entity
│   │   │   ├── SubmissionStatus.cs           # Enum
│   │   │   └── ProjectType.cs               # Value object
│   │   ├── Reviews/
│   │   │   ├── CodeReview.cs                 # Aggregate root
│   │   │   ├── CategoryScore.cs              # Entity
│   │   │   ├── ReviewVersion.cs              # Value object
│   │   │   └── ScoreCategory.cs              # Enum
│   │   ├── Candidates/
│   │   │   ├── Candidate.cs                  # Aggregate root
│   │   │   └── CandidateRole.cs              # Enum (Frontend/Backend)
│   │   ├── ScoringConfigs/
│   │   │   ├── ScoringConfig.cs              # Aggregate root
│   │   │   └── CategoryWeight.cs             # Entity
│   │   └── Users/
│   │       ├── User.cs                       # Aggregate root
│   │       └── UserRole.cs                   # Enum
│   ├── Common/
│   │   ├── Entity.cs                         # Base entity (Id, timestamps)
│   │   ├── AggregateRoot.cs                  # Base + domain events
│   │   ├── ValueObject.cs                    # Structural equality
│   │   ├── IDomainEvent.cs
│   │   └── IUnitOfWork.cs
│   ├── Events/
│   │   ├── SubmissionCreatedEvent.cs
│   │   ├── SubmissionProcessingStartedEvent.cs
│   │   ├── ReviewCompletedEvent.cs
│   │   └── ReviewFailedEvent.cs
│   └── Exceptions/
│       ├── DomainException.cs
│       ├── InvalidSubmissionException.cs
│       └── ScoringConfigNotFoundException.cs
│
├── TechTaskReview.Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IFileStorageService.cs
│   │   │   ├── IGitCloningService.cs
│   │   │   ├── IMalwareScannerService.cs
│   │   │   ├── IStaticAnalysisService.cs
│   │   │   ├── IAIReviewService.cs
│   │   │   ├── IAIProviderFactory.cs
│   │   │   ├── IPdfExportService.cs
│   │   │   ├── ISubmissionRepository.cs
│   │   │   ├── ICodeReviewRepository.cs
│   │   │   ├── ICandidateRepository.cs
│   │   │   ├── IScoringConfigRepository.cs
│   │   │   ├── IUserRepository.cs
│   │   │   ├── ICacheService.cs
│   │   │   └── ICurrentUserService.cs
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs         # MediatR pipeline behavior
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── PerformanceBehavior.cs
│   │   │   └── UnhandledExceptionBehavior.cs
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs
│   │   └── Models/
│   │       ├── PaginatedList.cs
│   │       ├── Result.cs                     # Operation result wrapper
│   │       └── AIReviewResult.cs
│   ├── Submissions/
│   │   ├── Commands/
│   │   │   ├── CreateSubmission/
│   │   │   │   ├── CreateSubmissionCommand.cs
│   │   │   │   ├── CreateSubmissionCommandHandler.cs
│   │   │   │   └── CreateSubmissionCommandValidator.cs
│   │   │   ├── CreateGitSubmission/
│   │   │   │   ├── CreateGitSubmissionCommand.cs
│   │   │   │   ├── CreateGitSubmissionCommandHandler.cs
│   │   │   │   └── CreateGitSubmissionCommandValidator.cs
│   │   │   └── RetrySubmission/
│   │   │       ├── RetrySubmissionCommand.cs
│   │   │       └── RetrySubmissionCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetSubmission/
│   │   │   │   ├── GetSubmissionQuery.cs
│   │   │   │   ├── GetSubmissionQueryHandler.cs
│   │   │   │   └── SubmissionDetailDto.cs
│   │   │   ├── GetSubmissions/
│   │   │   │   ├── GetSubmissionsQuery.cs
│   │   │   │   ├── GetSubmissionsQueryHandler.cs
│   │   │   │   └── SubmissionListDto.cs
│   │   │   └── GetSubmissionStatus/
│   │   │       ├── GetSubmissionStatusQuery.cs
│   │   │       └── GetSubmissionStatusQueryHandler.cs
│   │   └── EventHandlers/
│   │       ├── SubmissionCreatedEventHandler.cs   # Enqueues processing job
│   │       └── ReviewCompletedEventHandler.cs     # Sends notification
│   ├── Reviews/
│   │   ├── Commands/
│   │   │   └── ... (similar CQRS structure)
│   │   └── Queries/
│   │       ├── GetReview/
│   │       ├── GetReviewComparison/
│   │       └── ExportReviewPdf/
│   ├── Candidates/
│   │   ├── Commands/ ...
│   │   └── Queries/ ...
│   ├── ScoringConfigs/
│   │   ├── Commands/
│   │   │   ├── CreateScoringConfig/
│   │   │   └── UpdateScoringConfig/
│   │   └── Queries/
│   │       └── GetScoringConfig/
│   ├── Dashboard/
│   │   └── Queries/
│   │       ├── GetDashboardSummary/
│   │       └── GetCandidateRankings/
│   └── Users/
│       ├── Commands/ ...
│       └── Queries/ ...
│
├── TechTaskReview.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/          # EF Core entity configurations
│   │   │   ├── SubmissionConfiguration.cs
│   │   │   ├── CodeReviewConfiguration.cs
│   │   │   ├── CategoryScoreConfiguration.cs
│   │   │   ├── CandidateConfiguration.cs
│   │   │   ├── ScoringConfigConfiguration.cs
│   │   │   └── UserConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── SubmissionRepository.cs
│   │   │   ├── CodeReviewRepository.cs
│   │   │   ├── CandidateRepository.cs
│   │   │   ├── ScoringConfigRepository.cs
│   │   │   └── UserRepository.cs
│   │   ├── Migrations/
│   │   └── Interceptors/
│   │       ├── AuditableEntityInterceptor.cs
│   │       └── DomainEventDispatchInterceptor.cs
│   ├── FileStorage/
│   │   ├── MinioFileStorageService.cs
│   │   ├── LocalFileStorageService.cs     # Dev fallback
│   │   └── FileStorageOptions.cs
│   ├── Git/
│   │   ├── LibGit2SharpCloningService.cs
│   │   └── GitCloningOptions.cs
│   ├── Security/
│   │   ├── ClamAvScannerService.cs
│   │   └── MalwareScanOptions.cs
│   ├── StaticAnalysis/
│   │   ├── StaticAnalysisOrchestrator.cs
│   │   ├── Analyzers/
│   │   │   ├── ESLintAnalyzer.cs
│   │   │   ├── DotNetAnalyzer.cs
│   │   │   └── IStaticAnalyzer.cs
│   │   └── Models/
│   │       └── StaticAnalysisResult.cs
│   ├── AI/
│   │   ├── AIReviewService.cs
│   │   ├── Providers/
│   │   │   ├── IAIProvider.cs
│   │   │   ├── AIProviderFactory.cs
│   │   │   ├── OllamaProvider.cs         # Free tier
│   │   │   ├── OpenAIProvider.cs         # Paid upgrade
│   │   │   └── AnthropicProvider.cs      # Paid upgrade
│   │   ├── Chunking/
│   │   │   ├── IFileChunker.cs
│   │   │   ├── TokenAwareChunker.cs
│   │   │   └── ChunkingOptions.cs
│   │   ├── Prompts/
│   │   │   ├── PromptTemplateService.cs
│   │   │   ├── FrontendReviewPrompt.cs
│   │   │   └── BackendReviewPrompt.cs
│   │   └── Models/
│   │       ├── AIProviderOptions.cs
│   │       ├── AIReviewRequest.cs
│   │       └── AIReviewResponse.cs
│   ├── Caching/
│   │   └── RedisCacheService.cs
│   ├── Export/
│   │   ├── QuestPdfExportService.cs
│   │   └── Templates/
│   │       └── ReviewReportTemplate.cs
│   ├── BackgroundJobs/
│   │   ├── FileProcessingJob.cs
│   │   ├── StaticAnalysisJob.cs
│   │   ├── AIReviewJob.cs
│   │   ├── ScoreAggregationJob.cs
│   │   ├── PdfExportJob.cs
│   │   └── CleanupJob.cs               # Purge expired extracted repos
│   └── DependencyInjection.cs           # All infra registrations
│
├── TechTaskReview.Web/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── SubmissionsController.cs
│   │   ├── CandidatesController.cs
│   │   ├── ReviewsController.cs
│   │   ├── ScoringConfigsController.cs
│   │   ├── DashboardController.cs
│   │   └── ExportController.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── RequestLoggingMiddleware.cs
│   │   └── RateLimitingMiddleware.cs
│   ├── Filters/
│   │   └── ApiExceptionFilterAttribute.cs
│   ├── Models/
│   │   ├── ApiResponse.cs
│   │   └── ApiError.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── WebApplicationExtensions.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── tests/
    ├── TechTaskReview.Domain.Tests/
    │   ├── Aggregates/
    │   │   ├── SubmissionTests.cs
    │   │   ├── CodeReviewTests.cs
    │   │   └── ScoringConfigTests.cs
    │   └── Common/
    │       └── ValueObjectTests.cs
    ├── TechTaskReview.Application.Tests/
    │   ├── Submissions/
    │   │   ├── CreateSubmissionCommandTests.cs
    │   │   └── GetSubmissionQueryTests.cs
    │   └── Reviews/
    │       └── ...
    ├── TechTaskReview.Infrastructure.Tests/
    │   ├── AI/
    │   │   ├── TokenAwareChunkerTests.cs
    │   │   └── AIReviewServiceTests.cs
    │   └── StaticAnalysis/
    │       └── ...
    └── TechTaskReview.Web.Tests/
        └── Controllers/
            └── ... (integration tests)
```

## Pattern Implementations

### CQRS via MediatR

```csharp
// Command — write side
public record CreateSubmissionCommand(
    Guid CandidateId,
    Stream FileStream,
    string FileName,
    CandidateRole Role
) : IRequest<Result<Guid>>;

public class CreateSubmissionCommandHandler
    : IRequestHandler<CreateSubmissionCommand, Result<Guid>>
{
    private readonly ISubmissionRepository _submissions;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSubmissionCommandHandler(
        ISubmissionRepository submissions,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork)
    {
        _submissions = submissions;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateSubmissionCommand request,
        CancellationToken ct)
    {
        // 1. Store raw file
        var storagePath = await _fileStorage.StoreAsync(
            request.FileStream, request.FileName, ct);

        // 2. Create domain entity (raises SubmissionCreatedEvent)
        var submission = Submission.Create(
            request.CandidateId,
            storagePath,
            request.FileName,
            request.Role);

        // 3. Persist
        _submissions.Add(submission);
        await _unitOfWork.SaveChangesAsync(ct);

        // Domain event handler enqueues Hangfire job
        return Result<Guid>.Success(submission.Id);
    }
}
```

### FluentValidation

```csharp
public class CreateSubmissionCommandValidator
    : AbstractValidator<CreateSubmissionCommand>
{
    private static readonly string[] AllowedExtensions = [".zip", ".tar.gz", ".tgz"];
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB

    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty()
            .WithMessage("Candidate ID is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => AllowedExtensions.Any(ext =>
                name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"File must be one of: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileStream)
            .NotNull()
            .Must(stream => stream.Length <= MaxFileSize)
            .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)}MB.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Invalid candidate role.");
    }
}
```

### Repository Pattern

```csharp
// Domain layer — interface
public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Submission>> GetByCandidateIdAsync(Guid candidateId, CancellationToken ct);
    Task<PaginatedList<Submission>> GetPaginatedAsync(int page, int pageSize, CancellationToken ct);
    void Add(Submission submission);
    void Update(Submission submission);
}

// Infrastructure layer — implementation
public class SubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public SubmissionRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Submissions
            .Include(s => s.Files)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Add(Submission submission)
        => _context.Submissions.Add(submission);

    // ... other implementations
}
```

### Unit of Work

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWork(ApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving
        var domainEntities = _context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return await _context.SaveChangesAsync(ct);
    }
}
```

### AI Provider Abstraction

```csharp
public interface IAIProvider
{
    string Name { get; }
    int MaxTokens { get; }
    Task<AIReviewResponse> ReviewCodeAsync(AIReviewRequest request, CancellationToken ct);
    Task<bool> IsAvailableAsync(CancellationToken ct);
}

public interface IAIProviderFactory
{
    IAIProvider GetProvider(string? preferredProvider = null);
    IAIProvider GetFallbackProvider();
}

public class AIProviderFactory : IAIProviderFactory
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly AIProviderOptions _options;

    public IAIProvider GetProvider(string? preferredProvider = null)
    {
        var name = preferredProvider ?? _options.DefaultProvider;
        return _providers.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"AI provider '{name}' not registered.");
    }

    public IAIProvider GetFallbackProvider()
    {
        foreach (var provider in _providers.OrderBy(p => p.Name == _options.DefaultProvider ? 0 : 1))
        {
            if (provider.IsAvailableAsync(CancellationToken.None).GetAwaiter().GetResult())
                return provider;
        }
        throw new InvalidOperationException("No AI provider is available.");
    }
}
```

### Retry + Circuit Breaker (Polly)

```csharp
// Registered in DI
services.AddHttpClient<OllamaProvider>(client =>
{
    client.BaseAddress = new Uri(options.OllamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // LLM inference can be slow
})
.AddPolicyHandler(Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, delay, attempt, ctx) =>
        {
            Log.Warning("Retry {Attempt} for Ollama after {Delay}s", attempt, delay.TotalSeconds);
        }))
.AddPolicyHandler(Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromMinutes(1),
        onBreak: (_, duration) => Log.Error("Ollama circuit OPEN for {Duration}", duration),
        onReset: () => Log.Information("Ollama circuit CLOSED")));
```

## Dependency Injection Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistence
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ICodeReviewRepository, CodeReviewRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IScoringConfigRepository, ScoringConfigRepository>();

        // File storage
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        // Git
        services.AddScoped<IGitCloningService, LibGit2SharpCloningService>();

        // Malware scanning
        services.AddScoped<IMalwareScannerService, ClamAvScannerService>();

        // Static analysis
        services.AddScoped<IStaticAnalysisService, StaticAnalysisOrchestrator>();

        // AI
        services.Configure<AIProviderOptions>(configuration.GetSection("AI"));
        services.AddScoped<IAIProvider, OllamaProvider>();
        services.AddScoped<IAIProvider, OpenAIProvider>();
        services.AddScoped<IAIProviderFactory, AIProviderFactory>();
        services.AddScoped<IAIReviewService, AIReviewService>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddScoped<ICacheService, RedisCacheService>();

        // Background jobs
        services.AddHangfire(config => config
            .UseRedisStorage(configuration.GetConnectionString("Redis")));
        services.AddHangfireServer(options =>
        {
            options.Queues = ["critical", "default", "ai-review", "export"];
            options.WorkerCount = Environment.ProcessorCount * 2;
        });

        // PDF
        services.AddScoped<IPdfExportService, QuestPdfExportService>();

        return services;
    }
}
```

## Trade-offs

| Decision | Benefit | Cost |
|----------|---------|------|
| MediatR for CQRS | Decoupled handlers, pipeline behaviors | Indirection, harder to trace call flow |
| Hangfire over raw BackgroundService | Dashboard, retries, persistence, scheduling | Redis dependency, Hangfire license for some features |
| EF Core over Dapper | Migrations, change tracking, LINQ, entity configs | Heavier queries, N+1 risk (mitigated with explicit Includes) |
| Ollama as default AI | Free, local, no API keys | Slower inference, lower quality vs GPT-4/Claude, needs GPU |
| MinIO over local filesystem | S3-compatible, distributable | Extra container, operational overhead |
| Domain events via MediatR | Loose coupling between aggregates | In-process only (not distributed events) — acceptable for MVP |
