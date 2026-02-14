# 06 — AI Orchestration Strategy

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AIReviewService                           │
│                                                              │
│  ┌──────────┐   ┌──────────────┐   ┌───────────────────┐   │
│  │ File     │──▶│ Token-Aware  │──▶│ Prompt Template   │   │
│  │ Selector │   │ Chunker      │   │ Engine            │   │
│  └──────────┘   └──────────────┘   └───────────────────┘   │
│                                            │                 │
│                                            ▼                 │
│                 ┌──────────────────────────────────────┐     │
│                 │     Parallel Dispatch Engine          │     │
│                 │                                      │     │
│                 │  ┌─────────┐ ┌─────────┐ ┌────────┐│     │
│                 │  │ Chunk 1 │ │ Chunk 2 │ │Chunk N ││     │
│                 │  │ → AI    │ │ → AI    │ │→ AI    ││     │
│                 │  └─────────┘ └─────────┘ └────────┘│     │
│                 └──────────────────┬───────────────────┘     │
│                                    │                         │
│                                    ▼                         │
│                 ┌──────────────────────────────────────┐     │
│                 │     Result Aggregator                 │     │
│                 │     (merge partial reviews)           │     │
│                 └──────────────────┬───────────────────┘     │
│                                    │                         │
│                                    ▼                         │
│                 ┌──────────────────────────────────────┐     │
│                 │     Final Evaluation Pass             │     │
│                 │     (holistic summary + final scores) │     │
│                 └──────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

## Step 1: File Selection & Prioritization

Not all files deserve AI review. We prioritize to stay within token budgets and cost constraints.

```csharp
public class FileSelector
{
    public List<FileForReview> SelectFiles(
        IReadOnlyList<SubmissionFile> allFiles, ProjectType projectType)
    {
        var result = new List<FileForReview>();

        // Priority 1: Core source files (not test, not config)
        var coreFiles = allFiles
            .Where(f => !f.IsTestFile && !f.IsConfigFile)
            .Where(f => IsReviewableLanguage(f.Language))
            .OrderByDescending(f => f.LineCount) // Larger files often have more architecture signals
            .ToList();

        // Priority 2: Test files (sample — up to 20% of budget)
        var testFiles = allFiles
            .Where(f => f.IsTestFile)
            .Where(f => IsReviewableLanguage(f.Language))
            .OrderByDescending(f => f.LineCount)
            .Take(Math.Max(5, coreFiles.Count / 5))
            .ToList();

        // Priority 3: Config files (key configs only)
        var configFiles = allFiles
            .Where(f => f.IsConfigFile)
            .Where(f => IsImportantConfig(f.RelativePath))
            .ToList();

        foreach (var f in coreFiles)
            result.Add(new FileForReview(f, Priority.Core));
        foreach (var f in testFiles)
            result.Add(new FileForReview(f, Priority.Test));
        foreach (var f in configFiles)
            result.Add(new FileForReview(f, Priority.Config));

        return result;
    }

    private static bool IsReviewableLanguage(string lang) =>
        lang is "TypeScript" or "JavaScript" or "C#" or "Python" or "Java" or "Go";

    private static bool IsImportantConfig(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        return name is "tsconfig.json" or "package.json" or ".eslintrc.json"
            or "dockerfile" or "docker-compose.yml"
            || name.EndsWith(".csproj");
    }
}
```

## Step 2: Token-Aware Chunking

**Problem**: A 1000-file repo could have 200,000+ lines of code. No LLM can process this in one prompt.

**Solution**: Intelligent chunking that respects token limits AND semantic boundaries.

```csharp
public class TokenAwareChunker : IFileChunker
{
    private readonly ChunkingOptions _options;

    // Approximate tokens per character ratio (conservative)
    // For code: ~1 token per 3.5 characters (more dense than English prose)
    private const double TokensPerChar = 0.285;

    public List<ReviewChunk> CreateChunks(List<FileForReview> files)
    {
        var chunks = new List<ReviewChunk>();
        var currentChunk = new ReviewChunkBuilder(_options.MaxTokensPerChunk);

        // Group files by directory (related files should be reviewed together)
        var groupedFiles = files
            .GroupBy(f => GetLogicalGroup(f.File.RelativePath))
            .OrderByDescending(g => g.Sum(f => f.File.LineCount));

        foreach (var group in groupedFiles)
        {
            foreach (var file in group.OrderByDescending(f => f.Priority))
            {
                var content = File.ReadAllText(
                    Path.Combine(_options.ExtractedPath, file.File.RelativePath));

                var estimatedTokens = (int)(content.Length * TokensPerChar);

                // If a single file exceeds max chunk size, split it
                if (estimatedTokens > _options.MaxTokensPerChunk)
                {
                    var fileParts = SplitLargeFile(file, content);
                    foreach (var part in fileParts)
                    {
                        chunks.Add(part);
                    }
                    continue;
                }

                // Try to fit in current chunk
                if (!currentChunk.TryAdd(file, content, estimatedTokens))
                {
                    // Current chunk is full — finalize and start new one
                    chunks.Add(currentChunk.Build());
                    currentChunk = new ReviewChunkBuilder(_options.MaxTokensPerChunk);
                    currentChunk.TryAdd(file, content, estimatedTokens);
                }
            }
        }

        // Don't forget the last chunk
        if (currentChunk.HasFiles)
            chunks.Add(currentChunk.Build());

        return chunks;
    }

    private List<ReviewChunk> SplitLargeFile(FileForReview file, string content)
    {
        // Split on semantic boundaries: class/function definitions, blank lines
        var sections = SplitOnSemanticBoundaries(content, file.File.Language);
        var parts = new List<ReviewChunk>();
        var builder = new ReviewChunkBuilder(_options.MaxTokensPerChunk);

        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var tokens = (int)(section.Length * TokensPerChar);

            if (!builder.TryAddSection(file, section, tokens, partIndex: i))
            {
                parts.Add(builder.Build());
                builder = new ReviewChunkBuilder(_options.MaxTokensPerChunk);
                builder.TryAddSection(file, section, tokens, partIndex: i);
            }
        }

        if (builder.HasFiles) parts.Add(builder.Build());
        return parts;
    }

    private static List<string> SplitOnSemanticBoundaries(string content, string language)
    {
        // Split on: empty lines between top-level declarations
        // This keeps classes/functions intact when possible
        var pattern = language switch
        {
            "C#" => @"(?=\n\s*(?:public|private|protected|internal|namespace|class|interface|record|enum)\s)",
            "TypeScript" or "JavaScript" => @"(?=\n\s*(?:export|function|class|interface|type|const\s+\w+\s*=\s*(?:\(|function)))",
            _ => @"\n\n+" // fallback: split on double newlines
        };

        return Regex.Split(content, pattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string GetLogicalGroup(string relativePath)
    {
        // Group by first two directory levels
        var parts = relativePath.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? $"{parts[0]}/{parts[1]}" : parts[0];
    }
}
```

### Chunking Configuration

```csharp
public class ChunkingOptions
{
    // Ollama default context: 4096 tokens. We use ~3000 for code, rest for prompt + response.
    // For OpenAI/Anthropic: much larger contexts available.
    public int MaxTokensPerChunk { get; set; } = 3000;

    // Maximum chunks to process per submission (cost control)
    public int MaxChunksPerSubmission { get; set; } = 50;

    // If a repo exceeds max chunks, sample strategically
    public bool EnableSampling { get; set; } = true;

    public string ExtractedPath { get; set; } = null!;
}
```

### Sampling Strategy (for very large repos)

When a repo produces more chunks than `MaxChunksPerSubmission`:

1. **Always include**: entry points (`index.ts`, `Program.cs`, `App.tsx`, `main.py`)
2. **Always include**: largest 10 source files (architectural backbone)
3. **Always include**: up to 5 test files (test quality signal)
4. **Sample remaining**: random selection weighted by file size
5. **Log**: which files were sampled and which were skipped (for transparency)

## Step 3: Prompt Engineering

### Prompt Structure

Each chunk is sent with a structured prompt that enforces JSON output.

```csharp
public class PromptTemplateService
{
    public string BuildChunkReviewPrompt(
        ReviewChunk chunk,
        CandidateRole role,
        ProjectType projectType,
        string? staticAnalysisContext)
    {
        var roleContext = role == CandidateRole.Frontend
            ? FrontendContextBlock
            : BackendContextBlock;

        var categories = role == CandidateRole.Frontend
            ? FrontendCategories
            : BackendCategories;

        return $"""
            You are a senior {role} code reviewer evaluating a candidate's technical assessment submission.

            ## Context
            - **Project Type**: {projectType.PrimaryLanguage} / {projectType.Framework ?? "unknown framework"}
            - **Candidate Role**: {role}
            - **This is chunk {chunk.ChunkIndex + 1} of {chunk.TotalChunks}** from the repository.

            {roleContext}

            ## Files in this chunk
            {FormatFileList(chunk.Files)}

            ## Static Analysis Results (if available)
            {staticAnalysisContext ?? "No static analysis data for this chunk."}

            ## Code to Review
            ```
            {chunk.CombinedContent}
            ```

            ## Instructions
            Analyze the code above and provide scores for EACH applicable category.
            For categories that cannot be evaluated from this chunk, set score to null.

            You MUST respond with ONLY valid JSON in exactly this format:
            ```json
            {{
              "categories": [
                {FormatCategoryTemplate(categories)}
              ],
              "chunk_summary": "Brief summary of what this code does and key observations.",
              "critical_findings": ["List any critical bugs, security issues, or anti-patterns."]
            }}
            ```

            ## Scoring Guidelines
            - **0-2**: Fundamentally broken. Major bugs, security holes, or anti-patterns.
            - **3-4**: Below expectations. Significant issues that would require major refactoring.
            - **5-6**: Meets basic expectations. Functional but with notable room for improvement.
            - **7-8**: Good quality. Minor issues, generally well-structured.
            - **9-10**: Excellent. Production-ready, demonstrates senior-level practices.

            Be specific in justifications. Reference exact code patterns, line numbers, or function names.
            Do NOT give inflated scores. Most average code should score 5-6.
            """;
    }

    private const string FrontendContextBlock = """
        ## Frontend-Specific Review Focus
        Pay special attention to:
        - React component composition and reusability
        - State management patterns (lifting state, context, external stores)
        - Hook correctness (dependency arrays, custom hook extraction, rules of hooks)
        - Re-render optimization (React.memo, useMemo, useCallback — when justified)
        - Accessibility (semantic HTML, ARIA attributes, keyboard navigation)
        - Responsive design (media queries, fluid layouts, mobile-first)
        - CSS organization and specificity management
        - Bundle size awareness (lazy loading, code splitting)
        """;

    private const string BackendContextBlock = """
        ## Backend-Specific Review Focus
        Pay special attention to:
        - API design (REST conventions, status codes, request/response shapes)
        - Layered architecture adherence (controllers → services → repositories)
        - Dependency injection usage (constructor injection, interface abstractions)
        - Async/await correctness (no async void, proper cancellation token threading, no .Result/.Wait())
        - Security (input validation, auth checks, SQL injection prevention, CORS)
        - Database patterns (N+1 queries, transaction management, connection handling)
        - Error handling (global exception handling, domain exceptions, ProblemDetails)
        - Configuration management (options pattern, secrets handling)
        """;

    private static readonly string[] FrontendCategories =
    [
        "code_quality", "architecture", "maintainability", "naming",
        "error_handling", "security", "performance", "testability",
        "component_design", "state_management", "hooks_usage",
        "rerender_optimization", "accessibility", "responsiveness"
    ];

    private static readonly string[] BackendCategories =
    [
        "code_quality", "architecture", "maintainability", "naming",
        "error_handling", "security", "performance", "testability",
        "api_design", "layered_architecture", "dependency_injection",
        "async_correctness", "security_practices", "db_interaction_patterns"
    ];
}
```

### Sample AI Prompt (Frontend — rendered)

```
You are a senior Frontend code reviewer evaluating a candidate's technical assessment submission.

## Context
- **Project Type**: TypeScript / React
- **Candidate Role**: Frontend
- **This is chunk 3 of 12** from the repository.

## Frontend-Specific Review Focus
Pay special attention to:
- React component composition and reusability
- State management patterns (lifting state, context, external stores)
...

## Files in this chunk
- src/components/Dashboard/Dashboard.tsx (142 lines)
- src/components/Dashboard/DashboardCard.tsx (67 lines)
- src/hooks/useDashboardData.ts (45 lines)

## Code to Review
```tsx
// src/components/Dashboard/Dashboard.tsx
import React, { useEffect, useState } from 'react';
...
```

## Instructions
Analyze the code above and provide scores for EACH applicable category.
...
```

### Expected AI Response (JSON)

```json
{
  "categories": [
    {
      "name": "component_design",
      "score": 7.5,
      "justification": "Good component decomposition with Dashboard and DashboardCard separation. DashboardCard accepts typed props and is reasonably reusable. However, Dashboard.tsx has 142 lines with mixed data fetching and rendering logic that should be extracted.",
      "critical_issues": [],
      "refactor_suggestions": [
        "Extract data fetching logic from Dashboard into the existing useDashboardData hook — currently the hook exists but Dashboard still has inline fetch logic in useEffect.",
        "DashboardCard could accept a render prop or children for the card body instead of hardcoding the metric display format."
      ],
      "senior_improvement_plan": "Implement a container/presenter pattern: DashboardContainer handles data, Dashboard handles layout, DashboardCard handles individual metric display. Add error boundaries per card so one failing metric doesn't crash the entire dashboard.",
      "confidence": 0.85
    },
    {
      "name": "hooks_usage",
      "score": 5.0,
      "justification": "useDashboardData hook exists but is underutilized. The main Dashboard component has a useEffect with missing dependencies (ESLint would flag this). useState is used where useReducer would be more appropriate given the multiple related state transitions.",
      "critical_issues": [
        "useEffect on line 34 has [userId] in deps but also reads `filters` state without including it — stale closure bug."
      ],
      "refactor_suggestions": [
        "Add `filters` to useEffect dependency array or extract into useCallback.",
        "Consider useReducer for the loading/data/error state triple instead of three separate useState calls."
      ],
      "senior_improvement_plan": "Migrate all data fetching to TanStack Query (React Query). This eliminates manual loading/error state management, adds caching, and provides automatic background refetching. Custom hooks should compose React Query hooks rather than raw useEffect.",
      "confidence": 0.90
    }
  ],
  "chunk_summary": "Dashboard feature with main layout and metric cards. Data fetching is partially abstracted into a custom hook but with incomplete extraction. Component structure is reasonable but could benefit from stricter separation of concerns.",
  "critical_findings": [
    "Stale closure bug in Dashboard.tsx useEffect (line 34) — filters variable not in dependency array."
  ]
}
```

## Step 4: Parallel Dispatch

```csharp
public class AIReviewService : IAIReviewService
{
    private readonly IAIProviderFactory _providerFactory;
    private readonly IFileChunker _chunker;
    private readonly PromptTemplateService _promptService;
    private readonly AIProviderOptions _options;

    public async Task<AggregatedReviewResult> ReviewSubmissionAsync(
        Submission submission,
        List<FileForReview> files,
        string? staticAnalysisJson,
        CandidateRole role,
        CancellationToken ct)
    {
        var provider = _providerFactory.GetProvider();
        var chunks = _chunker.CreateChunks(files);

        // Apply max chunks limit
        if (chunks.Count > _options.MaxChunksPerSubmission)
        {
            chunks = SampleChunks(chunks, _options.MaxChunksPerSubmission);
        }

        // Tag each chunk with total count
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].ChunkIndex = i;
            chunks[i].TotalChunks = chunks.Count;
        }

        // Parallel dispatch with concurrency limit (respect rate limits)
        var semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests); // default: 3 for Ollama
        var chunkResults = new ConcurrentBag<ChunkReviewResult>();
        int failedChunks = 0;

        var tasks = chunks.Select(async chunk =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var prompt = _promptService.BuildChunkReviewPrompt(
                    chunk, role, submission.DetectedProjectType!, staticAnalysisJson);

                var response = await provider.ReviewCodeAsync(
                    new AIReviewRequest(prompt, _options.Temperature, _options.MaxResponseTokens), ct);

                var parsed = ParseChunkResponse(response.Content, chunk.ChunkIndex);
                chunkResults.Add(parsed);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedChunks);
                // Log but don't fail the entire review
                chunkResults.Add(ChunkReviewResult.Failed(chunk.ChunkIndex, ex.Message));
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);

        // Aggregate
        return AggregateResults(chunkResults.ToList(), failedChunks, chunks.Count);
    }
}
```

## Step 5: Result Aggregation

After all chunks are reviewed, we aggregate per-category scores.

```csharp
public class ResultAggregator
{
    public AggregatedReviewResult Aggregate(
        List<ChunkReviewResult> chunkResults, int failedChunks, int totalChunks)
    {
        var successfulResults = chunkResults.Where(r => !r.IsFailed).ToList();

        // Group scores by category across all chunks
        var categoryAggregates = successfulResults
            .SelectMany(r => r.Categories)
            .Where(c => c.Score.HasValue)
            .GroupBy(c => c.Name)
            .Select(g =>
            {
                var scores = g.ToList();

                // Weighted average: weight by confidence
                var weightedSum = scores.Sum(s => s.Score!.Value * s.Confidence);
                var totalWeight = scores.Sum(s => s.Confidence);
                var avgScore = totalWeight > 0 ? weightedSum / totalWeight : 0;

                // Collect all issues and suggestions (deduplicated)
                var criticalIssues = scores
                    .SelectMany(s => s.CriticalIssues)
                    .Distinct()
                    .ToArray();

                var suggestions = scores
                    .SelectMany(s => s.RefactorSuggestions)
                    .Distinct()
                    .ToArray();

                // Use the most detailed justification (longest, as a heuristic)
                var bestJustification = scores
                    .OrderByDescending(s => s.Justification.Length)
                    .First().Justification;

                // Senior plan: merge unique plans
                var seniorPlan = string.Join("\n\n", scores
                    .Select(s => s.SeniorImprovementPlan)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct());

                // Average confidence
                var avgConfidence = scores.Average(s => s.Confidence);

                return new AggregatedCategoryScore(
                    Name: g.Key,
                    Score: Math.Round(avgScore, 1),
                    Justification: bestJustification,
                    CriticalIssues: criticalIssues,
                    RefactorSuggestions: suggestions,
                    SeniorImprovementPlan: seniorPlan,
                    Confidence: Math.Round(avgConfidence, 2),
                    ChunksCovered: scores.Count
                );
            })
            .ToList();

        // Aggregate summary
        var allSummaries = successfulResults
            .Select(r => r.ChunkSummary)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var allCritical = successfulResults
            .SelectMany(r => r.CriticalFindings)
            .Distinct()
            .ToList();

        return new AggregatedReviewResult(
            Categories: categoryAggregates,
            ChunkSummaries: allSummaries,
            CriticalFindings: allCritical,
            ChunksProcessed: totalChunks - failedChunks,
            ChunksFailed: failedChunks
        );
    }
}
```

## Step 6: Final Evaluation Pass (Optional — V2)

For higher-quality results, a final pass sends the aggregated results back to the AI for a holistic summary:

```
Given these per-chunk reviews of a {role} candidate's code:

[aggregated scores and summaries]

Write a 3-paragraph executive summary covering:
1. Overall impression and strongest areas
2. Key concerns and areas for improvement
3. Hiring recommendation (Strong Yes / Yes / Maybe / No) with justification

Also verify that scores are consistent — if chunk reviews contradict each other, explain the discrepancy.
```

This is optional for MVP (adds latency and cost) but valuable for recruiter UX.

## Ollama Provider Implementation

```csharp
public class OllamaProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly AIProviderOptions _options;

    public string Name => "Ollama";
    public int MaxTokens => 4096; // Default Ollama context

    public async Task<AIReviewResponse> ReviewCodeAsync(
        AIReviewRequest request, CancellationToken ct)
    {
        var payload = new
        {
            model = _options.OllamaModel, // e.g., "deepseek-coder:6.7b" or "codellama:13b"
            prompt = request.Prompt,
            stream = false,
            options = new
            {
                temperature = request.Temperature,  // 0.1 — low for deterministic scoring
                num_predict = request.MaxResponseTokens,
                top_p = 0.9,
                repeat_penalty = 1.1
            },
            format = "json"  // Ollama JSON mode (forces valid JSON output)
        };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(ct);
        return new AIReviewResponse(result!.Response, result.TotalDuration);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
```

## Model Selection Rationale

| Model | Context | Speed | Quality | Cost | Use Case |
|-------|---------|-------|---------|------|----------|
| DeepSeek Coder 6.7B | 4K-16K | Fast | Good for code | Free (Ollama) | **MVP default** |
| CodeLlama 13B | 16K | Medium | Good | Free (Ollama) | MVP alternative (needs 16GB+ VRAM) |
| CodeLlama 34B | 16K | Slow | Very good | Free (Ollama) | Quality upgrade (needs 40GB+ VRAM) |
| GPT-4o-mini | 128K | Fast | Very good | ~$0.15/1M input | Paid tier option |
| Claude 3.5 Sonnet | 200K | Fast | Excellent | ~$3/1M input | Premium tier |

**MVP recommendation**: DeepSeek Coder 6.7B via Ollama. It runs on a single GPU with 8GB VRAM, produces structured JSON reliably, and handles code analysis well. Upgrade path is straightforward via the provider abstraction.

## Cost Estimation (at scale)

Assuming 100k submissions/month, average 20 chunks per submission:

| Provider | Tokens/submission | Cost/submission | Monthly cost |
|----------|------------------|-----------------|-------------|
| Ollama (local) | ~60K | $0 (hardware only) | GPU server: ~$500/mo |
| GPT-4o-mini | ~60K | ~$0.02 | ~$2,000/mo |
| Claude 3.5 Sonnet | ~60K | ~$0.36 | ~$36,000/mo |

Ollama is the obvious MVP choice. The hardware investment pays for itself within the first month compared to API costs.

## Token Budget Per Submission

```
Target: 60,000 tokens total per submission
├── Chunk prompts (input):  ~45,000 tokens (20 chunks × 2,250 avg)
├── Chunk responses (output): ~12,000 tokens (20 chunks × 600 avg)
├── Final summary (input):    ~2,000 tokens
└── Final summary (output):   ~1,000 tokens
```
