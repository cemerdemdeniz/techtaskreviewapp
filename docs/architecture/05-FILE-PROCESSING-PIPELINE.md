# 05 — File Processing Pipeline

## Pipeline Overview

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Upload  │───▶│ Extract  │───▶│ Malware  │───▶│ Static   │───▶│ AI       │
│  & Store │    │ & Detect │    │ Scan     │    │ Analysis │    │ Review   │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
     │               │               │               │               │
     ▼               ▼               ▼               ▼               ▼
  Uploaded       Extracting     MalwareScanning  StaticAnalysis   AIReview
                                      │                              │
                                      ▼                              ▼
                                 Quarantined              ┌──────────┐
                                 (if infected)            │ Scoring  │
                                                          │ & Store  │
                                                          └──────────┘
                                                               │
                                                               ▼
                                                          Completed
```

## Stage 1: Upload & Store

**Trigger**: HTTP POST to `/api/submissions` or `/api/submissions/git`

```
Input:  Multipart form data (zip file) OR JSON (git URL)
Output: Submission record (status: Uploaded), raw file in blob storage
```

### Zip Upload Flow

```csharp
public class FileProcessingJob
{
    // Stage 1 is handled synchronously in the command handler
    // The API controller receives the file, streams it to MinIO,
    // creates the Submission entity, and enqueues this background job.

    // This job handles stages 2-6 as a sequential pipeline.
    public async Task ExecuteAsync(Guid submissionId, CancellationToken ct)
    {
        var submission = await _submissions.GetByIdAsync(submissionId, ct)
            ?? throw new InvalidOperationException($"Submission {submissionId} not found.");

        try
        {
            // Stage 2: Extract & Detect
            await ExtractAndDetectAsync(submission, ct);

            // Stage 3: Malware Scan
            await MalwareScanAsync(submission, ct);

            // Stage 4: Static Analysis
            await RunStaticAnalysisAsync(submission, ct);

            // Stage 5-6: AI Review + Scoring (separate job for isolation)
            BackgroundJob.Enqueue<AIReviewJob>(
                job => job.ExecuteAsync(submissionId, ct));
        }
        catch (Exception ex)
        {
            submission.MarkFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            throw; // Hangfire will handle retry
        }
    }
}
```

### Git Repository Flow

```
1. Validate URL format (must be https://, no local paths, no file://)
2. Create Submission record (status: Uploaded)
3. Enqueue FileProcessingJob
4. FileProcessingJob clones repo to temp directory
5. Compresses to .tar.gz and stores in MinIO (for record-keeping)
6. Continues to Stage 2 with cloned directory
```

```csharp
// Git cloning with safety constraints
public class LibGit2SharpCloningService : IGitCloningService
{
    private readonly GitCloningOptions _options;

    public async Task<string> CloneAsync(string gitUrl, string? branch, CancellationToken ct)
    {
        // Safety: validate URL
        var uri = new Uri(gitUrl);
        if (uri.Scheme != "https")
            throw new InvalidSubmissionException("Only HTTPS git URLs are accepted.");
        if (uri.Host is "localhost" or "127.0.0.1" or "::1")
            throw new InvalidSubmissionException("Local repository URLs are not allowed.");

        var tempPath = Path.Combine(_options.TempDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        var cloneOptions = new CloneOptions
        {
            BranchName = branch ?? "main",
            RecurseSubmodules = false,     // Security: don't fetch submodules
            FetchOptions =
            {
                // Shallow clone — saves bandwidth and storage
                Depth = 1
            }
        };

        // Timeout: abort if clone takes > 5 minutes
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        await Task.Run(() => Repository.Clone(gitUrl, tempPath, cloneOptions), cts.Token);

        // Validate size after clone
        var dirSize = GetDirectorySize(tempPath);
        if (dirSize > _options.MaxRepoSizeBytes)
        {
            Directory.Delete(tempPath, recursive: true);
            throw new InvalidSubmissionException(
                $"Repository size ({dirSize / (1024 * 1024)}MB) exceeds maximum ({_options.MaxRepoSizeBytes / (1024 * 1024)}MB).");
        }

        return tempPath;
    }
}
```

## Stage 2: Extract & Detect Project Type

```
Input:  Raw .zip/.tar.gz in blob storage (or cloned repo path)
Output: Extracted directory, ProjectType, file inventory
```

### Extraction Rules

```csharp
public class ExtractionService
{
    private static readonly string[] DangerousExtensions =
        [".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".scr", ".com"];

    private static readonly string[] SkipDirectories =
        ["node_modules", ".git", "bin", "obj", "dist", "build", ".next",
         "__pycache__", ".venv", "vendor", "packages"];

    private const int MaxExtractedFiles = 10_000;
    private const long MaxExtractedSize = 500 * 1024 * 1024; // 500MB uncompressed

    public async Task<ExtractionResult> ExtractAsync(
        string archivePath, string outputDir, CancellationToken ct)
    {
        long totalSize = 0;
        int fileCount = 0;

        using var archive = ZipFile.OpenRead(archivePath);

        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();

            // Zip slip protection
            var destinationPath = Path.GetFullPath(Path.Combine(outputDir, entry.FullName));
            if (!destinationPath.StartsWith(Path.GetFullPath(outputDir)))
                throw new InvalidSubmissionException("Zip slip attack detected.");

            // Skip dangerous files
            if (DangerousExtensions.Any(ext =>
                entry.FullName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Skip junk directories
            if (SkipDirectories.Any(dir =>
                entry.FullName.Contains($"/{dir}/") || entry.FullName.Contains($"\\{dir}\\")))
                continue;

            // Enforce limits
            totalSize += entry.Length;
            if (totalSize > MaxExtractedSize)
                throw new InvalidSubmissionException("Extracted size exceeds 500MB limit.");

            fileCount++;
            if (fileCount > MaxExtractedFiles)
                throw new InvalidSubmissionException("Archive contains more than 10,000 files.");

            // Extract
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(destinationPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        return new ExtractionResult(outputDir, fileCount, totalSize);
    }
}
```

### Project Type Detection

```csharp
public class ProjectTypeDetector
{
    public ProjectType Detect(string extractedPath)
    {
        var files = Directory.GetFiles(extractedPath, "*.*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(extractedPath, f))
            .ToList();

        var rootFiles = Directory.GetFiles(extractedPath)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Detection heuristics (ordered by specificity)
        var buildFiles = new List<string>();
        string? primaryLanguage = null;
        string? framework = null;
        bool isMonorepo = false;

        // Check for monorepo indicators
        if (rootFiles.Contains("lerna.json") ||
            rootFiles.Contains("pnpm-workspace.yaml") ||
            rootFiles.Contains("nx.json") ||
            Directory.Exists(Path.Combine(extractedPath, "packages")))
        {
            isMonorepo = true;
        }

        // React / Next.js detection
        if (rootFiles.Contains("package.json"))
        {
            buildFiles.Add("package.json");
            var packageJson = File.ReadAllText(Path.Combine(extractedPath, "package.json"));

            if (packageJson.Contains("\"react\""))
            {
                primaryLanguage = files.Any(f => f.EndsWith(".tsx") || f.EndsWith(".ts"))
                    ? "TypeScript" : "JavaScript";

                framework = packageJson.Contains("\"next\"") ? "Next.js"
                    : packageJson.Contains("\"gatsby\"") ? "Gatsby"
                    : "React";
            }
            else if (packageJson.Contains("\"vue\""))
            {
                primaryLanguage = "JavaScript";
                framework = "Vue";
            }
            else if (packageJson.Contains("\"angular\"") || packageJson.Contains("\"@angular/core\""))
            {
                primaryLanguage = "TypeScript";
                framework = "Angular";
            }
            else
            {
                primaryLanguage = packageJson.Contains("\"typescript\"") ? "TypeScript" : "JavaScript";
                framework = "Node.js";
            }
        }

        // .NET detection
        var csprojFiles = files.Where(f => f.EndsWith(".csproj")).ToList();
        if (csprojFiles.Count != 0)
        {
            buildFiles.AddRange(csprojFiles);
            primaryLanguage ??= "C#";

            var anyCsproj = File.ReadAllText(Path.Combine(extractedPath, csprojFiles.First()));
            framework ??= anyCsproj.Contains("Microsoft.AspNetCore") ? "ASP.NET Core"
                : anyCsproj.Contains("Microsoft.NET.Sdk.Web") ? "ASP.NET Core"
                : ".NET";
        }

        // Fallback: count file extensions
        if (primaryLanguage is null)
        {
            var extensionCounts = files
                .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .ToList();

            var top = extensionCounts.FirstOrDefault()?.Key;
            primaryLanguage = top switch
            {
                ".py" => "Python",
                ".java" => "Java",
                ".go" => "Go",
                ".rs" => "Rust",
                ".rb" => "Ruby",
                ".php" => "PHP",
                _ => "Unknown"
            };
        }

        return new ProjectType(
            primaryLanguage ?? "Unknown",
            framework,
            buildFiles.ToArray(),
            isMonorepo);
    }
}
```

### File Inventory

After extraction, every source file is catalogued:

```csharp
public class FileInventoryService
{
    private static readonly Dictionary<string, string> ExtensionToLanguage = new()
    {
        [".ts"] = "TypeScript", [".tsx"] = "TypeScript",
        [".js"] = "JavaScript", [".jsx"] = "JavaScript",
        [".cs"] = "C#", [".py"] = "Python", [".java"] = "Java",
        [".go"] = "Go", [".rs"] = "Rust", [".rb"] = "Ruby",
        [".css"] = "CSS", [".scss"] = "SCSS", [".html"] = "HTML",
        [".json"] = "JSON", [".yaml"] = "YAML", [".yml"] = "YAML",
        [".md"] = "Markdown", [".sql"] = "SQL",
    };

    private static readonly string[] TestIndicators =
        ["test", "spec", "__tests__", "__mocks__", ".test.", ".spec."];

    private static readonly string[] ConfigIndicators =
        [".config.", "tsconfig", "eslint", "prettier", "webpack", "vite.config",
         "jest.config", "babel", ".env", "docker", "Dockerfile"];

    public List<SubmissionFile> BuildInventory(Guid submissionId, string extractedPath)
    {
        var results = new List<SubmissionFile>();

        foreach (var filePath in Directory.EnumerateFiles(extractedPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!ExtensionToLanguage.ContainsKey(ext)) continue;

            var relativePath = Path.GetRelativePath(extractedPath, filePath);
            var lineCount = File.ReadLines(filePath).Count();
            var fileInfo = new FileInfo(filePath);
            var lowerPath = relativePath.ToLowerInvariant();

            results.Add(SubmissionFile.Create(
                submissionId,
                relativePath,
                ExtensionToLanguage[ext],
                lineCount,
                fileInfo.Length,
                isTestFile: TestIndicators.Any(t => lowerPath.Contains(t)),
                isConfigFile: ConfigIndicators.Any(c => lowerPath.Contains(c))
            ));
        }

        return results;
    }
}
```

## Stage 3: Malware Scan

```
Input:  Extracted directory
Output: Clean (proceed) OR Quarantined (abort)
```

```csharp
public class ClamAvScannerService : IMalwareScannerService
{
    private readonly HttpClient _httpClient; // ClamAV REST API (clamav-rest container)

    public async Task<MalwareScanResult> ScanDirectoryAsync(string directoryPath, CancellationToken ct)
    {
        var findings = new List<string>();

        // Scan files in parallel batches (ClamAV handles one file per request)
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        var semaphore = new SemaphoreSlim(10); // Max 10 concurrent scans
        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                using var stream = File.OpenRead(file);
                using var content = new StreamContent(stream);
                var response = await _httpClient.PostAsync("/scan", content, ct);
                var result = await response.Content.ReadAsStringAsync(ct);

                if (result.Contains("FOUND"))
                {
                    lock (findings) { findings.Add($"{Path.GetFileName(file)}: {result}"); }
                }
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);

        return new MalwareScanResult(
            IsClean: findings.Count == 0,
            Findings: findings
        );
    }
}
```

**Trade-off**: ClamAV scanning 1000+ files adds latency (~30-60s for a typical repo). This is acceptable because the entire pipeline is background-processed. The candidate sees "Processing..." immediately, not a blocking wait.

## Stage 4: Static Analysis

```
Input:  Extracted directory + detected project type
Output: Structured static analysis results (JSON)
```

### Orchestrator

```csharp
public class StaticAnalysisOrchestrator : IStaticAnalysisService
{
    private readonly IEnumerable<IStaticAnalyzer> _analyzers;

    public async Task<StaticAnalysisResult> AnalyzeAsync(
        string extractedPath, ProjectType projectType, CancellationToken ct)
    {
        // Select relevant analyzers based on project type
        var applicableAnalyzers = _analyzers
            .Where(a => a.CanAnalyze(projectType))
            .ToList();

        var results = new List<AnalyzerOutput>();

        foreach (var analyzer in applicableAnalyzers)
        {
            try
            {
                var output = await analyzer.RunAsync(extractedPath, ct);
                results.Add(output);
            }
            catch (Exception ex)
            {
                // Static analysis failure is non-fatal — log and continue
                results.Add(AnalyzerOutput.Failed(analyzer.Name, ex.Message));
            }
        }

        return new StaticAnalysisResult(results);
    }
}
```

### ESLint Analyzer (Frontend)

```csharp
public class ESLintAnalyzer : IStaticAnalyzer
{
    public string Name => "ESLint";

    public bool CanAnalyze(ProjectType pt) =>
        pt.PrimaryLanguage is "TypeScript" or "JavaScript";

    public async Task<AnalyzerOutput> RunAsync(string path, CancellationToken ct)
    {
        // ESLint runs in a sandboxed Docker container for security
        // We mount the extracted repo read-only
        var process = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run --rm --read-only -v {path}:/code:ro " +
                        "techtask-eslint:latest npx eslint /code --format json --no-eslintrc " +
                        "--config /defaults/.eslintrc.json",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(process)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        // Parse ESLint JSON output
        var eslintResults = JsonSerializer.Deserialize<ESLintOutput[]>(stdout);

        return new AnalyzerOutput(
            AnalyzerName: Name,
            TotalIssues: eslintResults?.Sum(r => r.ErrorCount + r.WarningCount) ?? 0,
            Errors: eslintResults?.Sum(r => r.ErrorCount) ?? 0,
            Warnings: eslintResults?.Sum(r => r.WarningCount) ?? 0,
            RawOutput: stdout,
            Issues: MapToIssues(eslintResults)
        );
    }
}
```

### .NET Analyzer (Backend)

```csharp
public class DotNetAnalyzer : IStaticAnalyzer
{
    public string Name => "dotnet-analyzers";

    public bool CanAnalyze(ProjectType pt) => pt.PrimaryLanguage == "C#";

    public async Task<AnalyzerOutput> RunAsync(string path, CancellationToken ct)
    {
        // Run dotnet build with analyzers in Docker
        var process = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run --rm --read-only -v {path}:/code:ro " +
                        "techtask-dotnet:latest dotnet build /code " +
                        "-warnaserror- /p:TreatWarningsAsErrors=false " +
                        "-fl -flp:logfile=/tmp/build.log;verbosity=diagnostic",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(process)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        return ParseBuildOutput(stdout + stderr);
    }
}
```

## Stage 5-6: AI Review + Scoring

Detailed in documents [06-AI-ORCHESTRATION.md](06-AI-ORCHESTRATION.md) and [07-SCORING-MODEL.md](07-SCORING-MODEL.md).

## Pipeline Error Handling

| Stage | Failure Mode | Recovery |
|-------|-------------|----------|
| Upload | File too large / invalid type | Reject synchronously (400 response) |
| Extract | Corrupt archive | Mark Failed, notify recruiter, allow re-upload |
| Extract | Zip slip / path traversal | Mark Failed with security warning |
| Malware | Infected file | Mark Quarantined, alert admin, quarantine file |
| Malware | ClamAV unreachable | Retry 3x, then mark Failed (do NOT skip scan) |
| Static Analysis | Analyzer crash | Log warning, continue to AI review (non-fatal) |
| Static Analysis | Docker timeout | Log warning, continue (non-fatal) |
| AI Review | Provider unreachable | Retry 3x with exponential backoff, circuit breaker |
| AI Review | Token limit exceeded | Chunk already handles this; if single file too large, truncate + log |
| AI Review | Rate limited | Back off, retry from queue |
| Scoring | Incomplete AI results | Compute partial score if ≥60% categories available, mark Partial |

## Hangfire Job Configuration

```csharp
// Job configuration
public static class JobConfiguration
{
    public static void ConfigureJobs(IServiceProvider services)
    {
        var config = new BackgroundJobServerOptions
        {
            Queues = ["critical", "default", "ai-review", "export"],
            WorkerCount = Environment.ProcessorCount * 2,
            ServerTimeout = TimeSpan.FromMinutes(30),
            ShutdownTimeout = TimeSpan.FromMinutes(5),
        };

        // Retry policy per job type
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
        {
            Attempts = 3,
            DelaysInSeconds = [30, 120, 600], // 30s, 2min, 10min
            OnAttemptsExceeded = AttemptsExceededAction.Fail
        });

        // Recurring cleanup job
        RecurringJob.AddOrUpdate<CleanupJob>(
            "daily-cleanup",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(3, 0)); // 3 AM UTC
    }
}
```

## Pipeline Metrics (Observability)

Each stage emits:
- `pipeline.stage.duration` (histogram) — how long each stage takes
- `pipeline.stage.status` (counter) — success/failure counts per stage
- `pipeline.total.duration` (histogram) — end-to-end processing time
- `pipeline.queue.depth` (gauge) — number of pending jobs per queue

Target SLOs:
- P50 end-to-end: < 5 minutes
- P95 end-to-end: < 15 minutes
- P99 end-to-end: < 30 minutes
- Failure rate: < 2% (excluding malware quarantines)
