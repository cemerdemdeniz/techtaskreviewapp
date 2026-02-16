# TechTaskReview - AI-Powered Technical Assessment Platform

A production-grade SaaS platform for automated code review and candidate assessment. Supports **Frontend** and **Backend** engineer evaluations with AI-based scoring across 14+ dimensions.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start (Docker Compose)](#quick-start-docker-compose)
- [Local Development Setup](#local-development-setup)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [Configuration Reference](#configuration-reference)
- [API Endpoints](#api-endpoints)
- [Project Structure](#project-structure)
- [AI Model Setup (Ollama)](#ai-model-setup-ollama)
- [Database Migrations](#database-migrations)
- [Background Jobs](#background-jobs)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Architecture Documentation](#architecture-documentation)

---

## Architecture Overview

```
                    ┌─────────────┐
                    │   Nginx     │ :80
                    │  (Reverse   │
                    │   Proxy)    │
                    └──────┬──────┘
                 ┌─────────┴─────────┐
                 │                   │
          ┌──────▼──────┐    ┌───────▼──────┐
          │  React SPA  │    │  ASP.NET Core│
          │  (Vite)     │    │  Web API     │ :5000
          │  :5173      │    │  + Hangfire  │
          └─────────────┘    └──────┬───────┘
                                    │
              ┌──────────┬──────────┼──────────┬──────────┐
              │          │          │          │          │
        ┌─────▼───┐ ┌────▼───┐ ┌───▼────┐ ┌──▼───┐ ┌───▼────┐
        │PostgreSQL│ │ Redis  │ │ MinIO  │ │Ollama│ │ ClamAV │
        │  :5432   │ │ :6379  │ │:9000/01│ │:11434│ │ :3310  │
        └─────────┘ └────────┘ └────────┘ └──────┘ └────────┘
```

**Clean Architecture Layers:**
- **Domain** - Entities, Aggregates, Value Objects, Domain Events
- **Application** - CQRS (MediatR), Validators, DTOs, Interfaces
- **Infrastructure** - EF Core, File Storage, AI Providers, Background Jobs
- **Web** - REST Controllers, Middleware, JWT Auth

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend API | ASP.NET Core .NET 8 |
| Frontend | React 18 + TypeScript + Vite |
| Database | PostgreSQL 16 |
| Cache & Queues | Redis 7 |
| Object Storage | MinIO (S3-compatible) |
| AI Engine | Ollama (free-tier, local) |
| Malware Scanning | ClamAV |
| Background Jobs | Hangfire + Redis |
| PDF Export | QuestPDF |
| State Management | Zustand + TanStack Query |
| UI Styling | Tailwind CSS |
| Charts | Recharts |
| Containerization | Docker + Docker Compose |

---

## Prerequisites

### For Docker Compose (Recommended)
- [Docker](https://docs.docker.com/get-docker/) >= 24.0
- [Docker Compose](https://docs.docker.com/compose/install/) >= 2.20
- 8 GB RAM minimum (16 GB recommended for Ollama AI)
- 20 GB disk space

### For Local Development
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) >= 18.x (LTS recommended)
- [npm](https://www.npmjs.com/) >= 9.x
- [PostgreSQL 16](https://www.postgresql.org/download/)
- [Redis 7](https://redis.io/download/)
- [Ollama](https://ollama.ai/) (for AI features)

### Optional
- NVIDIA GPU + [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html) for GPU-accelerated AI inference
- [MinIO Client (mc)](https://min.io/docs/minio/linux/reference/minio-mc.html) for storage management

---

## Quick Start (Docker Compose)

### 1. Clone the Repository

```bash
git clone https://github.com/cemerdemdeniz/techtaskreviewapp.git
cd techtaskreviewapp
```

### 2. Configure Environment Variables

```bash
cp .env.example .env
```

Edit `.env` with your values:

```env
# Database password (change in production!)
DB_PASSWORD=your-secure-password-here

# JWT secret key (minimum 32 characters)
JWT_SECRET_KEY=your-jwt-secret-key-min-32-characters-long

# MinIO credentials
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=your-minio-secret-here

# Optional: Paid AI providers
OPENAI_API_KEY=
ANTHROPIC_API_KEY=
```

### 3. Start All Services

```bash
docker compose up -d
```

This starts 8 services: nginx, api, worker, postgres, redis, minio, ollama, clamav.

### 4. Download AI Model (First Time Only)

```bash
# Wait for ollama container to be ready (~30 seconds)
docker compose exec ollama ollama pull deepseek-coder:6.7b
```

> This downloads ~4 GB. For faster inference with GPU, use `codellama:13b` or `deepseek-coder:33b`.

### 5. Create MinIO Bucket

```bash
# Access MinIO Console at http://localhost:9001
# Login: minioadmin / minioadmin (or your configured credentials)
# Create bucket: techtask-submissions

# Or via CLI:
docker compose exec minio mc alias set local http://localhost:9000 minioadmin minioadmin
docker compose exec minio mc mb local/techtask-submissions
```

### 6. Access the Application

| Service | URL |
|---------|-----|
| Application | http://localhost |
| API (Swagger) | http://localhost:5000/swagger |
| Hangfire Dashboard | http://localhost:5000/hangfire |
| MinIO Console | http://localhost:9001 |

### 7. Stop Services

```bash
docker compose down          # Stop containers (keep data)
docker compose down -v       # Stop and remove all data volumes
```

---

## Local Development Setup

### Backend Setup

#### 1. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore TechTaskReview.sln
```

#### 2. Start External Services

Start PostgreSQL, Redis, MinIO, Ollama, and ClamAV. You can use Docker for just the dependencies:

```bash
# Start only infrastructure services (not the api/worker/nginx)
docker compose up -d postgres redis minio ollama clamav
```

#### 3. Configure Development Settings

The `appsettings.Development.json` is pre-configured for localhost connections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=techtask;Username=app;Password=devpassword123",
    "Redis": "localhost:6379"
  },
  "FileStorage": {
    "Endpoint": "localhost:9000"
  },
  "AI": {
    "OllamaBaseUrl": "http://localhost:11434"
  },
  "Malware": {
    "ClamAvUrl": "http://localhost:3310"
  }
}
```

#### 4. Apply Database Migrations

```bash
# Install EF Core tools (first time only)
dotnet tool install --global dotnet-ef

# Create initial migration
cd src/TechTaskReview.Infrastructure
dotnet ef migrations add InitialCreate -s ../TechTaskReview.Web/TechTaskReview.Web.csproj

# Apply migrations
dotnet ef database update -s ../TechTaskReview.Web/TechTaskReview.Web.csproj
cd ../..
```

> In Development mode, migrations are also auto-applied on startup via `Program.cs`.

#### 5. Run the API

```bash
cd src/TechTaskReview.Web
dotnet run
```

The API starts at `http://localhost:5000` with Swagger UI available at `http://localhost:5000/swagger`.

### Frontend Setup

#### 1. Install Dependencies

```bash
cd frontend
npm install
```

#### 2. Start Development Server

```bash
npm run dev
```

Frontend starts at `http://localhost:5173` with Hot Module Replacement (HMR).

#### 3. Build for Production

```bash
npm run build    # Output in frontend/dist/
npm run preview  # Preview production build locally
```

---

## Configuration Reference

### appsettings.json Sections

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| `ConnectionStrings` | `DefaultConnection` | `Host=postgres;...` | PostgreSQL connection string |
| `ConnectionStrings` | `Redis` | `redis:6379` | Redis connection string |
| `Jwt` | `SecretKey` | - | HMAC signing key (min 32 chars) |
| `Jwt` | `ExpirationMinutes` | `30` | JWT token lifetime |
| `FileStorage` | `Provider` | `MinIO` | Storage provider (`MinIO` or `Local`) |
| `FileStorage` | `Endpoint` | `minio:9000` | MinIO server endpoint |
| `FileStorage` | `BucketName` | `techtask-submissions` | S3 bucket name |
| `Git` | `TempDirectory` | `/tmp/git-clones` | Temp dir for git clones |
| `Git` | `MaxRepoSizeBytes` | `524288000` (500MB) | Maximum repo size |
| `Git` | `CloneTimeoutSeconds` | `300` | Git clone timeout |
| `AI` | `DefaultProvider` | `Ollama` | AI provider (`Ollama`, `OpenAI`) |
| `AI` | `OllamaModel` | `deepseek-coder:6.7b` | Ollama model name |
| `AI` | `MaxConcurrentRequests` | `3` | Parallel AI requests |
| `AI` | `Temperature` | `0.1` | AI temperature (0-1) |
| `Malware` | `ClamAvUrl` | `http://clamav:3310` | ClamAV daemon URL |
| `Cors` | `Origins` | `http://localhost:5173` | Allowed CORS origins |

---

## API Endpoints

### Authentication
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/login` | Login with email/password | No |
| POST | `/api/auth/register` | Register new user | No |
| GET | `/api/auth/me` | Get current user profile | Yes |

### Candidates
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/candidates` | List candidates (paginated, filterable) | Yes |
| GET | `/api/candidates/{id}` | Get candidate detail | Yes |
| POST | `/api/candidates` | Create new candidate | Yes |

### Submissions
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/submissions` | List submissions (paginated) | Yes |
| GET | `/api/submissions/{id}` | Get submission detail | Yes |
| GET | `/api/submissions/{id}/status` | Poll processing status | Yes |
| POST | `/api/submissions` | Upload ZIP submission (multipart) | Yes |
| POST | `/api/submissions/git` | Submit via Git URL | Yes |
| POST | `/api/submissions/{id}/retry` | Retry failed submission | Yes |

### Reviews
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/reviews/{id}` | Get review with all scores | Yes |
| GET | `/api/reviews/by-submission/{id}` | Get review by submission | Yes |
| GET | `/api/reviews/compare` | Compare candidates (query: candidateIds) | Yes |
| GET | `/api/reviews/{id}/export` | Export review as PDF | Yes |

### Dashboard
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/dashboard/summary` | Dashboard metrics (cached 5min) | Yes |

### Scoring Configuration
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/scoring-configs` | List scoring configurations | Admin |
| PUT | `/api/scoring-configs/{id}` | Update weights & thresholds | Admin |

---

## Project Structure

```
techtaskreviewapp/
├── docs/architecture/             # 14 detailed architecture documents
│   ├── 00-ARCHITECTURE-INDEX.md
│   ├── 01-SYSTEM-OVERVIEW.md
│   ├── 02-BACKEND-ARCHITECTURE.md
│   ├── 03-DOMAIN-MODEL.md
│   ├── 04-DATABASE-SCHEMA.md
│   ├── 05-FILE-PROCESSING-PIPELINE.md
│   ├── 06-AI-ORCHESTRATION.md
│   ├── 07-SCORING-MODEL.md
│   ├── 08-FRONTEND-ARCHITECTURE.md
│   ├── 09-API-CONTRACTS.md
│   ├── 10-SECURITY-MODEL.md
│   ├── 11-PERFORMANCE-SCALING.md
│   ├── 12-FAILURE-RECOVERY.md
│   └── 13-ROADMAP.md
├── src/
│   ├── TechTaskReview.Domain/           # Core business logic
│   │   ├── Aggregates/                  # DDD Aggregates
│   │   │   ├── Candidates/              # Candidate + CandidateRole
│   │   │   ├── Submissions/             # Submission state machine + files
│   │   │   ├── Reviews/                 # CodeReview + CategoryScore
│   │   │   ├── ScoringConfigs/          # Weight configuration
│   │   │   └── Users/                   # User + UserRole
│   │   ├── Common/                      # Entity, AggregateRoot, ValueObject
│   │   ├── Events/                      # Domain Events
│   │   └── Exceptions/                  # Domain Exceptions
│   ├── TechTaskReview.Application/      # Use cases (CQRS)
│   │   ├── Candidates/Commands+Queries/ # Candidate CRUD
│   │   ├── Submissions/Commands+Queries/# Submission CRUD + Retry
│   │   ├── Reviews/Queries/             # Review + Comparison
│   │   ├── Dashboard/Queries/           # Dashboard metrics
│   │   ├── ScoringConfigs/              # Config management
│   │   └── Common/                      # Interfaces, Models, Behaviors
│   ├── TechTaskReview.Infrastructure/   # External integrations
│   │   ├── AI/                          # AI orchestration + providers
│   │   │   ├── Providers/               # Ollama, OpenAI adapters
│   │   │   ├── Chunking/               # Token-aware code chunking
│   │   │   └── Prompts/                # Role-specific prompt templates
│   │   ├── BackgroundJobs/              # Hangfire jobs (processing pipeline)
│   │   ├── Caching/                     # Redis cache service
│   │   ├── Export/                      # QuestPDF report generation
│   │   ├── FileStorage/                 # MinIO + Local storage
│   │   ├── Git/                         # Git cloning with SSRF protection
│   │   ├── Persistence/                 # EF Core DbContext + Repositories
│   │   ├── Security/                    # ClamAV malware scanning
│   │   └── StaticAnalysis/              # ESLint + dotnet analyzers
│   └── TechTaskReview.Web/             # API layer
│       ├── Controllers/                 # 6 REST controllers
│       ├── Middleware/                   # Exception, Logging, Hangfire auth
│       ├── Services/                    # JWT + CurrentUser services
│       ├── Models/                      # API response wrapper
│       ├── Program.cs                   # App entry point
│       └── Dockerfile                   # Multi-stage Docker build
├── frontend/                            # React SPA
│   ├── src/
│   │   ├── app/                         # App shell, routes, providers
│   │   ├── features/                    # Feature modules
│   │   │   ├── auth/                    # Login, JWT, protected routes
│   │   │   ├── dashboard/               # Charts, stats, top candidates
│   │   │   ├── submissions/             # Upload, list, detail, retry
│   │   │   ├── reviews/                 # Scores, radar chart, comparison
│   │   │   ├── candidates/              # CRUD, filtering
│   │   │   └── scoring-config/          # Admin weight editor
│   │   ├── shared/                      # Reusable UI components
│   │   │   ├── components/ui/           # Button, Card, Badge, Modal, etc.
│   │   │   ├── components/layout/       # AppLayout, Header, Sidebar
│   │   │   ├── hooks/                   # useDebounce
│   │   │   └── utils/                   # formatters, constants
│   │   └── lib/                         # API client, query config
│   ├── package.json
│   ├── vite.config.ts
│   ├── tailwind.config.ts
│   └── tsconfig.json
├── tests/                               # Test projects
│   ├── TechTaskReview.Domain.Tests/
│   ├── TechTaskReview.Application.Tests/
│   ├── TechTaskReview.Infrastructure.Tests/
│   └── TechTaskReview.Web.Tests/
├── nginx/nginx.conf                     # Reverse proxy config
├── docker-compose.yml                   # 8-service orchestration
├── TechTaskReview.sln                   # .NET solution file
├── .env.example                         # Environment template
└── .gitignore
```

---

## AI Model Setup (Ollama)

### Recommended Models

| Model | Size | Speed | Quality | Use Case |
|-------|------|-------|---------|----------|
| `deepseek-coder:6.7b` | 4 GB | Fast | Good | Default - balanced |
| `codellama:7b` | 4 GB | Fast | Good | Alternative |
| `deepseek-coder:33b` | 19 GB | Slow | Excellent | High-quality reviews (GPU recommended) |
| `codellama:34b` | 19 GB | Slow | Excellent | High-quality alternative |

### Pull a Model

```bash
# Docker Compose
docker compose exec ollama ollama pull deepseek-coder:6.7b

# Local Ollama
ollama pull deepseek-coder:6.7b
```

### Change Model

Update `appsettings.json`:
```json
{
  "AI": {
    "OllamaModel": "deepseek-coder:33b"
  }
}
```

Or via environment variable:
```bash
AI__OllamaModel=deepseek-coder:33b
```

### GPU Acceleration

The `docker-compose.yml` includes NVIDIA GPU reservation for Ollama. Requirements:
1. NVIDIA GPU with CUDA support
2. [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html) installed
3. Docker configured with nvidia runtime

If you don't have a GPU, remove the `deploy.resources` section from the ollama service in `docker-compose.yml`:

```yaml
ollama:
  image: ollama/ollama:latest
  volumes:
    - ollamamodels:/root/.ollama
  ports:
    - "11434:11434"
  # Remove the deploy section if no GPU
  networks:
    - techtask-net
```

---

## Database Migrations

### Creating Migrations

```bash
cd src/TechTaskReview.Infrastructure

# Create a new migration
dotnet ef migrations add <MigrationName> \
  -s ../TechTaskReview.Web/TechTaskReview.Web.csproj

# Apply pending migrations
dotnet ef database update \
  -s ../TechTaskReview.Web/TechTaskReview.Web.csproj

# Revert last migration
dotnet ef migrations remove \
  -s ../TechTaskReview.Web/TechTaskReview.Web.csproj
```

### Auto-Migration (Development Only)

In `Development` environment, migrations are automatically applied on startup:

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

### Seeding Initial Data

After first migration, create an admin user via the API:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@techtask.local",
    "password": "Admin123!",
    "firstName": "System",
    "lastName": "Admin"
  }'
```

---

## Background Jobs

The platform uses **Hangfire** with Redis backing for background job processing.

### Processing Pipeline

When a submission is created, it triggers this 6-stage pipeline:

```
Upload → Extract → ClamAV Scan → Static Analysis → AI Review → Scoring
```

Each stage is a separate Hangfire job for retry isolation:

| Job | Class | Description |
|-----|-------|-------------|
| File Processing | `FileProcessingJob` | Extract ZIP/clone Git, scan malware, run static analysis |
| AI Review | `AIReviewJob` | Chunk code, dispatch to AI, aggregate scores |
| Daily Cleanup | `CleanupJob` | Remove old temp files, expired data (runs at 03:00 UTC) |

### Monitoring

Access the Hangfire Dashboard at: `http://localhost:5000/hangfire`

> Requires Admin role authentication.

---

## Testing

### Run All Tests

```bash
dotnet test TechTaskReview.sln
```

### Run Specific Test Project

```bash
dotnet test tests/TechTaskReview.Domain.Tests/
dotnet test tests/TechTaskReview.Application.Tests/
dotnet test tests/TechTaskReview.Infrastructure.Tests/
dotnet test tests/TechTaskReview.Web.Tests/
```

### Frontend Tests

```bash
cd frontend
npm run lint    # ESLint check
npm run build   # TypeScript compilation check
```

---

## Troubleshooting

### Common Issues

#### Docker Compose won't start
```bash
# Check service logs
docker compose logs api
docker compose logs postgres

# Ensure ports are free
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis
lsof -i :9000  # MinIO
```

#### Database connection refused
```bash
# Check PostgreSQL is healthy
docker compose ps postgres

# Manually test connection
docker compose exec postgres psql -U app -d techtask -c "SELECT 1"
```

#### Ollama model not responding
```bash
# Check Ollama is running
docker compose logs ollama

# Verify model is downloaded
docker compose exec ollama ollama list

# Test model directly
curl http://localhost:11434/api/generate \
  -d '{"model": "deepseek-coder:6.7b", "prompt": "Hello"}'
```

#### ClamAV signature update slow
ClamAV downloads virus signatures on first boot (~300 MB). This can take 5-10 minutes.
```bash
# Check ClamAV status
docker compose logs clamav
```

#### MinIO bucket not found
```bash
# Create bucket via MinIO Console (http://localhost:9001)
# Or via CLI:
docker compose exec minio mc alias set local http://localhost:9000 minioadmin minioadmin
docker compose exec minio mc mb local/techtask-submissions
```

#### Frontend can't connect to API
Ensure CORS is configured correctly in `appsettings.json`:
```json
{
  "Cors": {
    "Origins": "http://localhost:5173"
  }
}
```

#### Out of memory (Ollama)
Larger models require more RAM. Recommendations:
- `6.7b` models: 8 GB RAM
- `13b` models: 16 GB RAM
- `33b` models: 32 GB RAM + GPU recommended

---

## Architecture Documentation

Detailed architecture documentation is available in `docs/architecture/`:

| Document | Description |
|----------|-------------|
| [00 - Index](docs/architecture/00-ARCHITECTURE-INDEX.md) | Master index with assumptions |
| [01 - System Overview](docs/architecture/01-SYSTEM-OVERVIEW.md) | Component diagrams, deployment topology |
| [02 - Backend Architecture](docs/architecture/02-BACKEND-ARCHITECTURE.md) | Clean Architecture layers, patterns |
| [03 - Domain Model](docs/architecture/03-DOMAIN-MODEL.md) | Aggregates, entities, value objects |
| [04 - Database Schema](docs/architecture/04-DATABASE-SCHEMA.md) | Full DDL, indexes, retention |
| [05 - File Processing](docs/architecture/05-FILE-PROCESSING-PIPELINE.md) | 6-stage pipeline |
| [06 - AI Orchestration](docs/architecture/06-AI-ORCHESTRATION.md) | Chunking, prompts, aggregation |
| [07 - Scoring Model](docs/architecture/07-SCORING-MODEL.md) | Mathematical model, weights, bias mitigation |
| [08 - Frontend Architecture](docs/architecture/08-FRONTEND-ARCHITECTURE.md) | React structure, state management |
| [09 - API Contracts](docs/architecture/09-API-CONTRACTS.md) | Endpoint specs with JSON examples |
| [10 - Security Model](docs/architecture/10-SECURITY-MODEL.md) | Threat model, RBAC, validation |
| [11 - Performance & Scaling](docs/architecture/11-PERFORMANCE-SCALING.md) | Queue strategy, caching |
| [12 - Failure & Recovery](docs/architecture/12-FAILURE-RECOVERY.md) | Retry, circuit breakers |
| [13 - Roadmap](docs/architecture/13-ROADMAP.md) | MVP / V2 / V3 features |

---

## License

This project is proprietary software. All rights reserved.
