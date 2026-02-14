# 09 — API Contracts

## Base URL

```
https://api.techtaskreview.internal/api/v1
```

## Common Response Envelope

```typescript
// Success
{
  "success": true,
  "data": { ... }
}

// Error
{
  "success": false,
  "errors": [
    { "code": "VALIDATION_ERROR", "message": "File size exceeds 100MB limit.", "field": "file" }
  ]
}

// Paginated
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1547,
  "totalPages": 78
}
```

## Authentication

### POST /auth/login

```json
// Request
{
  "email": "recruiter@company.com",
  "password": "string"
}

// Response 200
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2026-02-15T10:00:00Z",
    "user": {
      "id": "a1b2c3d4-...",
      "email": "recruiter@company.com",
      "fullName": "Jane Smith",
      "role": "Recruiter"
    }
  }
}

// Response 401
{
  "success": false,
  "errors": [{ "code": "INVALID_CREDENTIALS", "message": "Invalid email or password." }]
}
```

## Candidates

### GET /candidates

Query params: `page`, `pageSize`, `role` (Frontend|Backend), `search` (name/email)

```json
// Response 200
{
  "items": [
    {
      "id": "c1d2e3f4-...",
      "fullName": "Alice Johnson",
      "email": "alice@example.com",
      "role": "Frontend",
      "position": "Senior Frontend Engineer",
      "submissionCount": 2,
      "latestScore": 7.85,
      "latestStatus": "Completed",
      "createdAt": "2026-01-15T09:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 342,
  "totalPages": 18
}
```

### POST /candidates

```json
// Request
{
  "fullName": "Alice Johnson",
  "email": "alice@example.com",
  "role": "Frontend",
  "position": "Senior Frontend Engineer",
  "notes": "Referred by engineering team"
}

// Response 201
{
  "success": true,
  "data": { "id": "c1d2e3f4-..." }
}
```

## Submissions

### POST /submissions (Zip Upload)

Content-Type: `multipart/form-data`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `candidateId` | UUID | Yes | |
| `file` | Binary | Yes | .zip, .tar.gz, .tgz (max 100MB) |

```json
// Response 202 Accepted
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "status": "Uploaded",
    "statusUrl": "/api/v1/submissions/s1a2b3c4-.../status"
  }
}

// Response 400
{
  "success": false,
  "errors": [
    { "code": "VALIDATION_ERROR", "message": "File must be one of: .zip, .tar.gz, .tgz", "field": "file" }
  ]
}
```

### POST /submissions/git

```json
// Request
{
  "candidateId": "c1d2e3f4-...",
  "gitUrl": "https://github.com/alice/assessment-project",
  "branch": "main"
}

// Response 202 Accepted
{
  "success": true,
  "data": {
    "id": "s2b3c4d5-...",
    "status": "Uploaded",
    "statusUrl": "/api/v1/submissions/s2b3c4d5-.../status"
  }
}
```

### GET /submissions/:id/status

Designed for polling. Lightweight response.

```json
// Response 200 (processing)
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "status": "AIReview",
    "processingStartedAt": "2026-02-14T10:05:00Z",
    "estimatedProgress": 0.65,
    "currentStage": "AI code review (chunk 13 of 20)"
  }
}

// Response 200 (completed)
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "status": "Completed",
    "processingStartedAt": "2026-02-14T10:05:00Z",
    "processingCompletedAt": "2026-02-14T10:12:34Z",
    "reviewId": "r3c4d5e6-..."
  }
}

// Response 200 (failed)
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "status": "Failed",
    "failureReason": "Archive extraction failed: corrupt zip file header."
  }
}
```

### GET /submissions/:id

Full submission detail.

```json
// Response 200
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "candidateId": "c1d2e3f4-...",
    "candidateName": "Alice Johnson",
    "source": "ZipUpload",
    "originalFileName": "assessment.zip",
    "fileSizeBytes": 2456789,
    "status": "Completed",
    "detectedProject": {
      "primaryLanguage": "TypeScript",
      "framework": "React",
      "buildFiles": ["package.json", "tsconfig.json"],
      "isMonorepo": false
    },
    "totalFiles": 87,
    "totalLinesOfCode": 4521,
    "processingStartedAt": "2026-02-14T10:05:00Z",
    "processingCompletedAt": "2026-02-14T10:12:34Z",
    "reviewId": "r3c4d5e6-...",
    "createdAt": "2026-02-14T10:04:30Z"
  }
}
```

### POST /submissions/:id/retry

Re-process a failed submission.

```json
// Response 202 Accepted
{
  "success": true,
  "data": {
    "id": "s1a2b3c4-...",
    "status": "Uploaded"
  }
}
```

## Reviews

### GET /reviews/:id

```json
// Response 200
{
  "success": true,
  "data": {
    "id": "r3c4d5e6-...",
    "submissionId": "s1a2b3c4-...",
    "candidateId": "c1d2e3f4-...",
    "candidateName": "Alice Johnson",
    "version": "1.0-v1.0",
    "aiProvider": "Ollama",
    "aiModel": "deepseek-coder:6.7b",
    "status": "Completed",
    "weightedTotalScore": 7.85,
    "summary": "Well-structured React application with good component decomposition. State management is clean with appropriate use of custom hooks. Main areas for improvement are accessibility (missing ARIA attributes in interactive elements) and test coverage (no integration tests found). Error handling in async operations is inconsistent — some components swallow errors silently.",
    "scoringConfigName": "Frontend Default v1",
    "categoryScores": [
      {
        "category": "CodeQuality",
        "categoryLabel": "Code Quality",
        "score": 8.2,
        "weight": 0.12,
        "weightedContribution": 0.984,
        "confidence": 0.88,
        "justification": "Clean, readable code with consistent formatting. Good use of TypeScript strict mode. Minimal any usage (found in 2 utility functions). ESLint shows 0 errors, 3 warnings.",
        "criticalIssues": [],
        "refactorSuggestions": [
          "Replace the 2 `any` usages in utils/api.ts with proper generic types.",
          "Consider using branded types for IDs (UserId, ProductId) to prevent mixing."
        ],
        "seniorImprovementPlan": "Adopt stricter TypeScript compiler options (noUncheckedIndexedAccess, exactOptionalPropertyTypes). Implement a custom ESLint plugin for project-specific patterns. Add Prettier for formatting consistency.",
        "isPassingMinimum": true
      },
      {
        "category": "ComponentDesign",
        "categoryLabel": "Component Design",
        "score": 7.5,
        "weight": 0.12,
        "weightedContribution": 0.9,
        "confidence": 0.85,
        "justification": "Good separation between container and presentational components. Props interfaces are well-defined. Some components (Dashboard.tsx at 180 lines) could be decomposed further.",
        "criticalIssues": [],
        "refactorSuggestions": [
          "Split Dashboard.tsx into DashboardLayout, DashboardMetrics, DashboardCharts.",
          "Extract reusable DataTable from the 3 different table implementations."
        ],
        "seniorImprovementPlan": "Implement compound component pattern for complex UI (e.g., Form with Form.Field, Form.Error). Use render props or children for maximum flexibility. Add Storybook for component documentation.",
        "isPassingMinimum": true
      },
      {
        "category": "Accessibility",
        "categoryLabel": "Accessibility",
        "score": 4.0,
        "weight": 0.04,
        "weightedContribution": 0.16,
        "confidence": 0.92,
        "justification": "Semantic HTML is partially used (main, nav, header present) but interactive elements lack ARIA attributes. No skip navigation link. Color contrast issues in secondary text.",
        "criticalIssues": [
          "Clickable div elements without role='button' and keyboard handlers in ProductCard.tsx",
          "Form inputs missing associated labels in CheckoutForm.tsx",
          "No aria-live region for dynamic content updates"
        ],
        "refactorSuggestions": [
          "Replace clickable divs with button elements or add role + onKeyDown.",
          "Add htmlFor to all label elements, or wrap inputs in labels.",
          "Add aria-live='polite' region for cart update notifications."
        ],
        "seniorImprovementPlan": "Integrate axe-core for automated accessibility testing. Add keyboard navigation tests with @testing-library. Conduct a WCAG 2.1 AA audit. Consider using Radix UI primitives for accessible-by-default interactive components.",
        "isPassingMinimum": false,
        "minimumRequired": 2.0
      }
    ],
    "staticAnalysis": {
      "eslint": {
        "errors": 0,
        "warnings": 3,
        "topIssues": ["no-explicit-any (2x)", "react-hooks/exhaustive-deps (1x)"]
      }
    },
    "processingDuration": "PT7M34S",
    "chunksProcessed": 18,
    "chunksFailed": 0,
    "createdAt": "2026-02-14T10:12:34Z"
  }
}
```

### GET /reviews/compare?candidateA={id}&candidateB={id}

```json
// Response 200
{
  "success": true,
  "data": {
    "candidateA": {
      "id": "c1d2e3f4-...",
      "name": "Alice Johnson",
      "totalScore": 7.85,
      "reviewVersion": "1.0-v1.0"
    },
    "candidateB": {
      "id": "c9d8e7f6-...",
      "name": "Bob Williams",
      "totalScore": 6.42,
      "reviewVersion": "1.0-v1.0"
    },
    "versionMatch": true,
    "categories": [
      {
        "category": "CodeQuality",
        "label": "Code Quality",
        "scoreA": 8.2,
        "scoreB": 6.5,
        "difference": 1.7,
        "winner": "A"
      },
      {
        "category": "ComponentDesign",
        "label": "Component Design",
        "scoreA": 7.5,
        "scoreB": 7.8,
        "difference": -0.3,
        "winner": "B"
      }
    ]
  }
}
```

### POST /reviews/:id/export/pdf

```json
// Response 202 Accepted
{
  "success": true,
  "data": {
    "jobId": "j4d5e6f7-...",
    "statusUrl": "/api/v1/export/j4d5e6f7-.../status"
  }
}

// GET /export/:jobId/status — Response 200 (ready)
{
  "success": true,
  "data": {
    "status": "Ready",
    "downloadUrl": "/api/v1/export/j4d5e6f7-.../download",
    "expiresAt": "2026-02-15T10:12:34Z"
  }
}
```

## Dashboard

### GET /dashboard/summary

```json
// Response 200
{
  "success": true,
  "data": {
    "totalCandidates": 342,
    "totalSubmissions": 567,
    "completedReviews": 498,
    "pendingReviews": 23,
    "failedReviews": 12,
    "averageScore": 6.34,
    "averageProcessingTime": "PT8M12S",
    "scoreDistribution": {
      "0-2": 12,
      "2-4": 45,
      "4-6": 189,
      "6-8": 201,
      "8-10": 51
    },
    "recentSubmissions": [
      {
        "id": "s1a2b3c4-...",
        "candidateName": "Alice Johnson",
        "status": "Completed",
        "score": 7.85,
        "createdAt": "2026-02-14T10:04:30Z"
      }
    ]
  }
}
```

### GET /dashboard/rankings

Query params: `role`, `scoringConfigId`, `limit` (default 20)

```json
// Response 200
{
  "items": [
    {
      "rank": 1,
      "candidateId": "c1d2e3f4-...",
      "candidateName": "Alice Johnson",
      "role": "Frontend",
      "totalScore": 7.85,
      "topStrength": "Code Quality (8.2)",
      "topWeakness": "Accessibility (4.0)",
      "reviewedAt": "2026-02-14T10:12:34Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 205,
  "totalPages": 11
}
```

## Scoring Configs

### GET /scoring-configs

```json
// Response 200
{
  "items": [
    {
      "id": "sc1a2b3c-...",
      "name": "Frontend Default v1",
      "targetRole": "Frontend",
      "isDefault": true,
      "version": 1,
      "weightCount": 14,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

### PUT /scoring-configs/:id

```json
// Request
{
  "name": "Frontend Default v2",
  "weights": [
    { "category": "CodeQuality", "weight": 0.12, "minimumPassingScore": null },
    { "category": "ComponentDesign", "weight": 0.14, "minimumPassingScore": null },
    { "category": "Security", "weight": 0.06, "minimumPassingScore": 3.0 },
    { "category": "Accessibility", "weight": 0.06, "minimumPassingScore": 2.0 }
  ]
}

// Response 200
{
  "success": true,
  "data": {
    "id": "sc1a2b3c-...",
    "version": 2
  }
}
```

## Error Codes

| HTTP Status | Code | When |
|-------------|------|------|
| 400 | VALIDATION_ERROR | Request body fails FluentValidation |
| 401 | UNAUTHORIZED | Missing or expired JWT |
| 403 | FORBIDDEN | Valid JWT but insufficient role |
| 404 | NOT_FOUND | Entity doesn't exist |
| 409 | CONFLICT | Duplicate email, concurrent modification |
| 413 | PAYLOAD_TOO_LARGE | File exceeds 100MB |
| 422 | UNPROCESSABLE | Valid format but semantically invalid |
| 429 | RATE_LIMITED | Too many requests |
| 500 | INTERNAL_ERROR | Unhandled server error |
| 503 | SERVICE_UNAVAILABLE | AI provider down (circuit open) |
