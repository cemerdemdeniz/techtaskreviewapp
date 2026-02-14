# 03 — Domain Model

## Aggregate Map

```
┌─────────────────────────────────────────────────────────────────┐
│                        DOMAIN MODEL                              │
│                                                                  │
│  ┌──────────────┐    1    ┌──────────────┐                      │
│  │  Candidate   │────────*│  Submission   │                      │
│  │  (Aggregate) │         │  (Aggregate)  │                      │
│  │              │         │               │                      │
│  │  - Name      │         │  - StoragePath│    1   ┌───────────┐│
│  │  - Email     │         │  - FileName   │───────*│Submission ││
│  │  - Role      │         │  - Status     │        │File       ││
│  │  - Position  │         │  - ProjectType│        │(Entity)   ││
│  │  - CreatedAt │         │  - FileSize   │        │- Path     ││
│  └──────────────┘         │  - GitUrl     │        │- Language ││
│                           │  - CreatedAt  │        │- Size     ││
│                           └───────┬───────┘        └───────────┘│
│                                   │                              │
│                                   │ 1                            │
│                                   ▼                              │
│                           ┌───────────────┐                      │
│                           │  CodeReview    │                      │
│                           │  (Aggregate)   │                      │
│                           │                │   1   ┌────────────┐│
│                           │  - Version     │──────*│Category    ││
│                           │  - Provider    │       │Score       ││
│                           │  - Model       │       │(Entity)    ││
│                           │  - Status      │       │            ││
│                           │  - WeightedScr │       │- Category  ││
│                           │  - RawJSON     │       │- Score     ││
│                           │  - Summary     │       │- Justific. ││
│                           │  - CreatedAt   │       │- Issues    ││
│                           └───────────────┘       │- Suggests. ││
│                                                    │- SeniorPlan││
│  ┌──────────────┐                                  └────────────┘│
│  │ScoringConfig │   1    ┌──────────────┐                        │
│  │(Aggregate)   │───────*│CategoryWeight│                        │
│  │              │        │(Entity)      │                        │
│  │  - Name      │        │              │                        │
│  │  - Role      │        │  - Category  │                        │
│  │  - IsDefault │        │  - Weight    │                        │
│  │  - Version   │        │  - MinScore  │                        │
│  └──────────────┘        └──────────────┘                        │
│                                                                  │
│  ┌──────────────┐                                                │
│  │  User        │                                                │
│  │  (Aggregate) │                                                │
│  │              │                                                │
│  │  - Email     │                                                │
│  │  - Name      │                                                │
│  │  - Role      │                                                │
│  │  - PassHash  │                                                │
│  └──────────────┘                                                │
└──────────────────────────────────────────────────────────────────┘
```

## Entity Definitions

### Base Types

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
}

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other || GetType() != other.GetType())
            return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component?.GetHashCode() ?? 0));
}
```

### Candidate Aggregate

```csharp
public enum CandidateRole
{
    Frontend = 1,
    Backend = 2
}

public class Candidate : AggregateRoot
{
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public CandidateRole Role { get; private set; }
    public string? Position { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<Submission> _submissions = [];
    public IReadOnlyList<Submission> Submissions => _submissions.AsReadOnly();

    private Candidate() { } // EF Core

    public static Candidate Create(string fullName, string email, CandidateRole role, string? position = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidSubmissionException("Full name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidSubmissionException("Email is required.");

        return new Candidate
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Position = position
        };
    }
}
```

### Submission Aggregate

```csharp
public enum SubmissionStatus
{
    Uploaded = 1,
    Extracting = 2,
    MalwareScanning = 3,
    StaticAnalysis = 4,
    AIReview = 5,
    Scoring = 6,
    Completed = 7,
    Failed = 8,
    Quarantined = 9   // Malware detected
}

public enum SubmissionSource
{
    ZipUpload = 1,
    GitRepository = 2
}

public class ProjectType : ValueObject
{
    public string PrimaryLanguage { get; }        // "TypeScript", "C#"
    public string? Framework { get; }              // "React", "ASP.NET Core"
    public string[] BuildFiles { get; }            // ["package.json"], [".csproj"]
    public bool IsMonorepo { get; }

    public ProjectType(string primaryLanguage, string? framework, string[] buildFiles, bool isMonorepo)
    {
        PrimaryLanguage = primaryLanguage;
        Framework = framework;
        BuildFiles = buildFiles;
        IsMonorepo = isMonorepo;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PrimaryLanguage;
        yield return Framework;
        yield return IsMonorepo;
    }
}

public class Submission : AggregateRoot
{
    public Guid CandidateId { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public SubmissionSource Source { get; private set; }
    public string? GitUrl { get; private set; }
    public string? GitBranch { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public ProjectType? DetectedProjectType { get; private set; }
    public string? ExtractedPath { get; private set; }
    public int? TotalFiles { get; private set; }
    public int? TotalLinesOfCode { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessingCompletedAt { get; private set; }

    private readonly List<SubmissionFile> _files = [];
    public IReadOnlyList<SubmissionFile> Files => _files.AsReadOnly();

    public CodeReview? Review { get; private set; }

    private Submission() { }

    public static Submission CreateFromUpload(
        Guid candidateId, string storagePath, string fileName, long fileSize, CandidateRole role)
    {
        var submission = new Submission
        {
            CandidateId = candidateId,
            StoragePath = storagePath,
            OriginalFileName = fileName,
            FileSizeBytes = fileSize,
            Source = SubmissionSource.ZipUpload,
            Status = SubmissionStatus.Uploaded
        };

        submission.RaiseDomainEvent(new SubmissionCreatedEvent(submission.Id, candidateId, role));
        return submission;
    }

    public static Submission CreateFromGit(
        Guid candidateId, string gitUrl, string? branch, CandidateRole role)
    {
        var submission = new Submission
        {
            CandidateId = candidateId,
            GitUrl = gitUrl,
            GitBranch = branch ?? "main",
            OriginalFileName = new Uri(gitUrl).Segments.Last().TrimEnd('/'),
            Source = SubmissionSource.GitRepository,
            Status = SubmissionStatus.Uploaded
        };

        submission.RaiseDomainEvent(new SubmissionCreatedEvent(submission.Id, candidateId, role));
        return submission;
    }

    public void MarkExtracting() => TransitionTo(SubmissionStatus.Extracting);
    public void MarkMalwareScanning() => TransitionTo(SubmissionStatus.MalwareScanning);
    public void MarkStaticAnalysis() => TransitionTo(SubmissionStatus.StaticAnalysis);
    public void MarkAIReview() => TransitionTo(SubmissionStatus.AIReview);
    public void MarkScoring() => TransitionTo(SubmissionStatus.Scoring);

    public void MarkCompleted()
    {
        TransitionTo(SubmissionStatus.Completed);
        ProcessingCompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewCompletedEvent(Id, CandidateId));
    }

    public void MarkFailed(string reason)
    {
        Status = SubmissionStatus.Failed;
        FailureReason = reason;
        ProcessingCompletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReviewFailedEvent(Id, CandidateId, reason));
    }

    public void MarkQuarantined(string reason)
    {
        Status = SubmissionStatus.Quarantined;
        FailureReason = $"MALWARE DETECTED: {reason}";
    }

    public void SetExtractionResults(string extractedPath, ProjectType projectType, int totalFiles, int totalLoc)
    {
        ExtractedPath = extractedPath;
        DetectedProjectType = projectType;
        TotalFiles = totalFiles;
        TotalLinesOfCode = totalLoc;
    }

    public void AddFile(SubmissionFile file) => _files.Add(file);

    private void TransitionTo(SubmissionStatus newStatus)
    {
        // Validate state transitions
        var validTransitions = new Dictionary<SubmissionStatus, SubmissionStatus[]>
        {
            [SubmissionStatus.Uploaded] = [SubmissionStatus.Extracting, SubmissionStatus.Failed],
            [SubmissionStatus.Extracting] = [SubmissionStatus.MalwareScanning, SubmissionStatus.Failed],
            [SubmissionStatus.MalwareScanning] = [SubmissionStatus.StaticAnalysis, SubmissionStatus.Quarantined, SubmissionStatus.Failed],
            [SubmissionStatus.StaticAnalysis] = [SubmissionStatus.AIReview, SubmissionStatus.Failed],
            [SubmissionStatus.AIReview] = [SubmissionStatus.Scoring, SubmissionStatus.Failed],
            [SubmissionStatus.Scoring] = [SubmissionStatus.Completed, SubmissionStatus.Failed],
        };

        if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new DomainException($"Cannot transition from {Status} to {newStatus}.");

        if (Status == SubmissionStatus.Uploaded)
            ProcessingStartedAt = DateTime.UtcNow;

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SubmissionFile : Entity
{
    public Guid SubmissionId { get; private set; }
    public string RelativePath { get; private set; } = null!;
    public string Language { get; private set; } = null!;
    public int LineCount { get; private set; }
    public long SizeBytes { get; private set; }
    public bool IsTestFile { get; private set; }
    public bool IsConfigFile { get; private set; }

    private SubmissionFile() { }

    public static SubmissionFile Create(
        Guid submissionId, string relativePath, string language,
        int lineCount, long sizeBytes, bool isTestFile, bool isConfigFile)
    {
        return new SubmissionFile
        {
            SubmissionId = submissionId,
            RelativePath = relativePath,
            Language = language,
            LineCount = lineCount,
            SizeBytes = sizeBytes,
            IsTestFile = isTestFile,
            IsConfigFile = isConfigFile
        };
    }
}
```

### CodeReview Aggregate

```csharp
public enum ReviewStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Partial = 4     // Some chunks failed but enough data for scoring
}

public class ReviewVersion : ValueObject
{
    public int Major { get; }
    public int Minor { get; }
    public string ScoringModelVersion { get; }

    public ReviewVersion(int major, int minor, string scoringModelVersion)
    {
        Major = major;
        Minor = minor;
        ScoringModelVersion = scoringModelVersion;
    }

    public static ReviewVersion Current => new(1, 0, "v1.0");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return ScoringModelVersion;
    }

    public override string ToString() => $"{Major}.{Minor}-{ScoringModelVersion}";
}

public class CodeReview : AggregateRoot
{
    public Guid SubmissionId { get; private set; }
    public ReviewVersion Version { get; private set; } = null!;
    public string AIProviderName { get; private set; } = null!;
    public string AIModelName { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public decimal WeightedTotalScore { get; private set; }
    public string? Summary { get; private set; }
    public string? RawResponseJson { get; private set; }
    public string? StaticAnalysisResultsJson { get; private set; }
    public int ChunksProcessed { get; private set; }
    public int ChunksFailed { get; private set; }
    public TimeSpan ProcessingDuration { get; private set; }
    public Guid ScoringConfigId { get; private set; }

    private readonly List<CategoryScore> _categoryScores = [];
    public IReadOnlyList<CategoryScore> CategoryScores => _categoryScores.AsReadOnly();

    private CodeReview() { }

    public static CodeReview Create(
        Guid submissionId, string providerName, string modelName, Guid scoringConfigId)
    {
        return new CodeReview
        {
            SubmissionId = submissionId,
            Version = ReviewVersion.Current,
            AIProviderName = providerName,
            AIModelName = modelName,
            Status = ReviewStatus.InProgress,
            ScoringConfigId = scoringConfigId
        };
    }

    public void AddCategoryScore(CategoryScore score)
    {
        if (_categoryScores.Any(s => s.Category == score.Category))
            throw new DomainException($"Score for category {score.Category} already exists.");
        _categoryScores.Add(score);
    }

    public void Complete(decimal weightedTotal, string summary, string rawJson, TimeSpan duration,
        int chunksProcessed, int chunksFailed)
    {
        WeightedTotalScore = weightedTotal;
        Summary = summary;
        RawResponseJson = rawJson;
        ProcessingDuration = duration;
        ChunksProcessed = chunksProcessed;
        ChunksFailed = chunksFailed;
        Status = chunksFailed > 0 ? ReviewStatus.Partial : ReviewStatus.Completed;
    }

    public void Fail(string rawJson, TimeSpan duration)
    {
        RawResponseJson = rawJson;
        ProcessingDuration = duration;
        Status = ReviewStatus.Failed;
    }

    public void SetStaticAnalysisResults(string json)
        => StaticAnalysisResultsJson = json;
}
```

### CategoryScore Entity

```csharp
public enum ScoreCategory
{
    // General (applicable to both)
    CodeQuality = 1,
    Architecture = 2,
    Maintainability = 3,
    Naming = 4,
    ErrorHandling = 5,
    Security = 6,
    Performance = 7,
    Testability = 8,

    // Frontend-specific
    ComponentDesign = 100,
    StateManagement = 101,
    HooksUsage = 102,
    RerenderOptimization = 103,
    Accessibility = 104,
    Responsiveness = 105,

    // Backend-specific
    ApiDesign = 200,
    LayeredArchitecture = 201,
    DependencyInjection = 202,
    AsyncCorrectness = 203,
    SecurityPractices = 204,
    DbInteractionPatterns = 205
}

public class CategoryScore : Entity
{
    public Guid CodeReviewId { get; private set; }
    public ScoreCategory Category { get; private set; }
    public decimal Score { get; private set; }              // 0.0 - 10.0
    public string Justification { get; private set; } = null!;
    public string[] CriticalIssues { get; private set; } = [];
    public string[] RefactorSuggestions { get; private set; } = [];
    public string SeniorLevelImprovementPlan { get; private set; } = null!;
    public decimal Confidence { get; private set; }         // 0.0 - 1.0 (AI self-reported)

    private CategoryScore() { }

    public static CategoryScore Create(
        Guid codeReviewId,
        ScoreCategory category,
        decimal score,
        string justification,
        string[] criticalIssues,
        string[] refactorSuggestions,
        string seniorPlan,
        decimal confidence)
    {
        if (score < 0 || score > 10)
            throw new DomainException($"Score must be between 0 and 10. Got: {score}");
        if (confidence < 0 || confidence > 1)
            throw new DomainException($"Confidence must be between 0 and 1. Got: {confidence}");

        return new CategoryScore
        {
            CodeReviewId = codeReviewId,
            Category = category,
            Score = Math.Round(score, 1),
            Justification = justification,
            CriticalIssues = criticalIssues,
            RefactorSuggestions = refactorSuggestions,
            SeniorLevelImprovementPlan = seniorPlan,
            Confidence = Math.Round(confidence, 2)
        };
    }
}
```

### ScoringConfig Aggregate

```csharp
public class ScoringConfig : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public CandidateRole TargetRole { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }
    public string? Description { get; private set; }

    private readonly List<CategoryWeight> _weights = [];
    public IReadOnlyList<CategoryWeight> Weights => _weights.AsReadOnly();

    private ScoringConfig() { }

    public static ScoringConfig Create(string name, CandidateRole targetRole, string? description = null)
    {
        return new ScoringConfig
        {
            Name = name,
            TargetRole = targetRole,
            Version = 1,
            Description = description
        };
    }

    public void AddWeight(ScoreCategory category, decimal weight, decimal? minimumPassingScore = null)
    {
        if (weight < 0 || weight > 1)
            throw new DomainException($"Weight must be between 0 and 1. Got: {weight}");

        _weights.Add(new CategoryWeight
        {
            ScoringConfigId = Id,
            Category = category,
            Weight = weight,
            MinimumPassingScore = minimumPassingScore
        });
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public decimal ComputeWeightedScore(IReadOnlyList<CategoryScore> scores)
    {
        decimal weightedSum = 0;
        decimal totalWeight = 0;

        foreach (var weight in _weights)
        {
            var score = scores.FirstOrDefault(s => s.Category == weight.Category);
            if (score is not null)
            {
                weightedSum += score.Score * weight.Weight;
                totalWeight += weight.Weight;
            }
        }

        return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0;
    }

    public List<(ScoreCategory Category, decimal Score)> GetFailingCategories(IReadOnlyList<CategoryScore> scores)
    {
        return _weights
            .Where(w => w.MinimumPassingScore.HasValue)
            .Select(w => (w.Category, Score: scores.FirstOrDefault(s => s.Category == w.Category)?.Score ?? 0))
            .Where(x => x.Score < _weights.First(w => w.Category == x.Category).MinimumPassingScore!.Value)
            .ToList();
    }
}

public class CategoryWeight : Entity
{
    public Guid ScoringConfigId { get; set; }
    public ScoreCategory Category { get; set; }
    public decimal Weight { get; set; }           // 0.0 - 1.0 (relative weight)
    public decimal? MinimumPassingScore { get; set; }  // Optional floor (e.g., Security must be >= 5)
}
```

### User Aggregate

```csharp
public enum UserRole
{
    Admin = 1,
    Recruiter = 2,
    Reviewer = 3
}

public class User : AggregateRoot
{
    public string Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    public static User Create(string email, string fullName, string passwordHash, UserRole role)
    {
        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
}
```

### Domain Events

```csharp
public interface IDomainEvent : INotification { }

public record SubmissionCreatedEvent(Guid SubmissionId, Guid CandidateId, CandidateRole Role) : IDomainEvent;
public record SubmissionProcessingStartedEvent(Guid SubmissionId) : IDomainEvent;
public record ReviewCompletedEvent(Guid SubmissionId, Guid CandidateId) : IDomainEvent;
public record ReviewFailedEvent(Guid SubmissionId, Guid CandidateId, string Reason) : IDomainEvent;
```

### Audit Log (Infrastructure concern, not domain)

```csharp
// Not a domain entity — lives in infrastructure
public class AuditLogEntry
{
    public long Id { get; set; }              // Bigint auto-increment for audit perf
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }    // JSON
    public string? NewValues { get; set; }    // JSON
    public string? IpAddress { get; set; }
}
```
