# 01 — System Overview

## High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           CLIENTS                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │
│  │ Recruiter UI │  │ Admin Panel  │  │ Candidate    │                  │
│  │ (React SPA)  │  │ (React SPA)  │  │ Upload Portal│                  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘                  │
└─────────┼──────────────────┼──────────────────┼─────────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        EDGE / GATEWAY                                   │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Nginx Reverse Proxy + Rate Limiter + TLS Termination           │   │
│  │  - Rate limit: 100 req/min per IP (API), 10 uploads/min        │   │
│  │  - Max upload body: 100MB                                       │   │
│  │  - CORS: whitelist SPA origin only                              │   │
│  └──────────────────────────┬───────────────────────────────────────┘   │
└─────────────────────────────┼───────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION TIER                                   │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐     │
│  │              ASP.NET Core Web API (.NET 8)                     │     │
│  │                                                                │     │
│  │  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐    │     │
│  │  │ Auth     │ │ Submis-  │ │ Scoring   │ │ Reports      │    │     │
│  │  │ Module   │ │ sion API │ │ Config API│ │ Export API   │    │     │
│  │  └──────────┘ └──────────┘ └───────────┘ └──────────────┘    │     │
│  │                                                                │     │
│  │  ┌──────────┐ ┌──────────┐ ┌───────────┐                     │     │
│  │  │ Candi-   │ │ Dashboard│ │ Admin     │                     │     │
│  │  │ date API │ │ API      │ │ API       │                     │     │
│  │  └──────────┘ └──────────┘ └───────────┘                     │     │
│  └────────────────────────────────────────────────────────────────┘     │
│                              │                                          │
│                              ▼                                          │
│  ┌────────────────────────────────────────────────────────────────┐     │
│  │           Background Processing (Hangfire)                     │     │
│  │                                                                │     │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐      │     │
│  │  │ File         │ │ Static       │ │ AI Review        │      │     │
│  │  │ Processing   │ │ Analysis     │ │ Orchestrator     │      │     │
│  │  │ Worker       │ │ Worker       │ │ Worker           │      │     │
│  │  └──────────────┘ └──────────────┘ └──────────────────┘      │     │
│  │                                                                │     │
│  │  ┌──────────────┐ ┌──────────────┐                            │     │
│  │  │ PDF Export   │ │ Cleanup      │                            │     │
│  │  │ Worker       │ │ Worker       │                            │     │
│  │  └──────────────┘ └──────────────┘                            │     │
│  └────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────┘
          │              │               │
          ▼              ▼               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        DATA TIER                                        │
│                                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐      │
│  │ PostgreSQL   │  │ Redis        │  │ File Storage             │      │
│  │              │  │              │  │ (Local/MinIO/S3)         │      │
│  │ - Submissions│  │ - Job queues │  │                          │      │
│  │ - Scores     │  │ - Rate limit │  │ - Raw uploads (.zip)    │      │
│  │ - Users      │  │ - Sessions   │  │ - Extracted repos       │      │
│  │ - Configs    │  │ - Cache      │  │ - Cloned repos          │      │
│  │ - Audit logs │  │              │  │ - Generated PDFs        │      │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘      │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │              AI Provider Layer                                │       │
│  │                                                               │       │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │       │
│  │  │ Ollama   │  │ OpenAI   │  │ Anthropic│  │ Azure    │    │       │
│  │  │ (Local)  │  │ (Paid)   │  │ (Paid)   │  │ OpenAI   │    │       │
│  │  │ FREE     │  │ Upgrade  │  │ Upgrade  │  │ Upgrade  │    │       │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │       │
│  └──────────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | Scaling Strategy |
|-----------|---------------|-----------------|
| Nginx | TLS, rate limiting, static asset serving, request routing | Horizontal (multiple instances behind LB) |
| ASP.NET Core API | HTTP request handling, auth, validation, command dispatch | Horizontal (stateless, N instances) |
| Hangfire Workers | Background job execution (file processing, AI review, PDF) | Vertical (more threads) + Horizontal (more worker nodes) |
| PostgreSQL | Persistent storage, ACID transactions, audit trail | Vertical first, read replicas at scale |
| Redis | Caching, distributed locking, rate limit counters, Hangfire queue backing | Vertical, then Redis Cluster |
| File Storage | Binary artifact storage (uploads, repos, PDFs) | MinIO for MVP, S3 for production |
| Ollama | Local LLM inference for code review | GPU-bound, vertical scaling |

## Request Flow — Submission Lifecycle

```
Candidate uploads .zip
        │
        ▼
[1] API validates (size, type, auth) ──── reject if invalid
        │
        ▼
[2] Raw file stored to blob storage
        │
        ▼
[3] Submission record created (status: Uploaded)
        │
        ▼
[4] Hangfire job enqueued: FileProcessingJob
        │
        ▼
[5] Worker extracts archive ──── status: Extracting
        │
        ▼
[6] Project type detected (.csproj, package.json, etc.) ──── status: Analyzing
        │
        ▼
[7] Malware scan (ClamAV) ──── reject + quarantine if infected
        │
        ▼
[8] Static analysis (ESLint/dotnet analyzers) ──── status: StaticAnalysis
        │
        ▼
[9] File chunking for AI ──── status: AIReview
        │
        ▼
[10] AI review batches dispatched (parallel where rate limits allow)
        │
        ▼
[11] Partial results aggregated ──── status: Scoring
        │
        ▼
[12] Weighted scores computed and persisted ──── status: Completed
        │
        ▼
[13] Recruiter notified (email/webhook/dashboard update)
```

## Deployment Topology — MVP (Docker Compose)

```yaml
# docker-compose.yml (logical view)
services:
  nginx:           # Reverse proxy + TLS
  api:             # ASP.NET Core API (ports 5000-5001)
  worker:          # Hangfire worker (same binary, different entry point)
  postgres:        # PostgreSQL 16
  redis:           # Redis 7
  minio:           # S3-compatible object storage
  ollama:          # Local LLM server
  clamav:          # Antivirus scanner
  frontend:        # React SPA (served by nginx in prod)
```

## Deployment Topology — Production (Kubernetes)

```
┌─────────────────────────────────────────────────────┐
│  Kubernetes Cluster                                  │
│                                                      │
│  Namespace: techtask-prod                            │
│                                                      │
│  ┌─────────────┐  ┌─────────────┐                   │
│  │ Ingress     │  │ API Deploy  │ (3 replicas)      │
│  │ Controller  │──│ HPA: 3-10   │                   │
│  └─────────────┘  └─────────────┘                   │
│                                                      │
│  ┌─────────────┐  ┌─────────────┐                   │
│  │ Worker      │  │ Ollama      │ (GPU nodes)       │
│  │ Deploy      │  │ StatefulSet │                   │
│  │ HPA: 2-8   │  │ 1-4 replicas│                   │
│  └─────────────┘  └─────────────┘                   │
│                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │
│  │ PostgreSQL  │  │ Redis       │  │ MinIO      │  │
│  │ Operator    │  │ Sentinel    │  │ Operator   │  │
│  └─────────────┘  └─────────────┘  └────────────┘  │
└─────────────────────────────────────────────────────┘
```

## Technology Choices — Rationale

| Decision | Choice | Rationale | Alternatives Considered |
|----------|--------|-----------|------------------------|
| Web framework | ASP.NET Core 8 | Constraint. High throughput, mature ecosystem, native AOT option | — |
| ORM | EF Core 8 | First-class .NET integration, migrations, LINQ | Dapper (considered for read-side, may use both) |
| Job queue | Hangfire + Redis | Persistent queues, dashboard, retry policies, .NET native | MassTransit (overkill for MVP), raw Redis streams |
| Cache | Redis | Already needed for Hangfire, versatile | In-memory only (not distributed-safe) |
| File storage | MinIO | S3-compatible, self-hosted, free | Local filesystem (not scalable), actual S3 (cost) |
| AI inference | Ollama | Free, local, supports CodeLlama/DeepSeek | LM Studio (less API-friendly), vLLM (more complex) |
| Static analysis | ESLint + dotnet analyzers | Industry standard, parseable JSON output | SonarQube (heavy for MVP) |
| Malware scan | ClamAV | Free, open-source, container-ready | commercial AV (cost) |
| PDF generation | QuestPDF | .NET native, no external dependencies, fluent API | wkhtmltopdf (deprecated), Puppeteer (heavy) |
| Frontend | React 18 + TypeScript | Constraint. Massive ecosystem | — |
| State management | Zustand | Lightweight, TypeScript-first, no boilerplate | Redux Toolkit (heavier), Jotai |
| Data fetching | TanStack Query (React Query) | Caching, deduplication, background refresh | SWR (less features), RTK Query |
| CSS | Tailwind CSS 3 | Utility-first, fast iteration, small bundle | CSS Modules, styled-components |
| Charts | Recharts | React-native, composable, lightweight | Chart.js (imperative), D3 (overkill) |
| Auth | JWT + ASP.NET Identity | Standards-based, stateless API auth | OAuth2/OIDC via Keycloak (V2) |
