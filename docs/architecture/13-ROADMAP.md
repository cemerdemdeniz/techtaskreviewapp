# 13 — MVP vs V2 Roadmap

## MVP Scope (V1)

**Goal**: Working end-to-end pipeline that accepts submissions, analyzes code, and presents scores to recruiters.

### MVP Features

| Feature | Priority | Status |
|---------|----------|--------|
| Candidate CRUD | P0 | MVP |
| Zip file upload + extraction | P0 | MVP |
| Git repository cloning | P0 | MVP |
| ClamAV malware scanning | P0 | MVP |
| ESLint static analysis (frontend) | P0 | MVP |
| dotnet analyzer (backend) | P0 | MVP |
| AI code review via Ollama (DeepSeek Coder 6.7B) | P0 | MVP |
| Token-aware file chunking | P0 | MVP |
| Weighted scoring computation | P0 | MVP |
| Default scoring configs (Frontend + Backend) | P0 | MVP |
| Review detail page with category scores | P0 | MVP |
| Recruiter dashboard (summary + recent) | P0 | MVP |
| JWT authentication + RBAC (Admin, Recruiter) | P0 | MVP |
| Processing progress polling UI | P0 | MVP |
| Candidate comparison (2 candidates, table) | P1 | MVP |
| PDF export (basic) | P1 | MVP |
| Audit logging | P1 | MVP |
| Scoring config editor (admin) | P1 | MVP |
| Retry failed submissions | P1 | MVP |
| Docker Compose deployment | P0 | MVP |

### MVP Non-Goals (Explicitly Deferred)

- Multi-tenant architecture
- Real-time WebSocket updates
- Email notifications
- Bulk import/export
- Custom AI model fine-tuning
- On-premise deployment package
- Mobile-responsive recruiter UI (desktop-first)
- Candidate self-service portal (submissions are admin-initiated)
- Internationalization

### MVP Technical Constraints

- Single PostgreSQL instance (no read replicas)
- Single Ollama instance (one GPU)
- Local MinIO (single node)
- Single Hangfire worker process
- No Kubernetes — Docker Compose only
- No CI/CD pipeline (manual deploy)

### MVP Deployment

```yaml
# docker-compose.yml
version: '3.8'
services:
  nginx:
    image: nginx:alpine
    ports: ["80:80", "443:443"]
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./frontend/dist:/usr/share/nginx/html

  api:
    build: ./src/TechTaskReview.Web
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=techtask;Username=app;Password=${DB_PASSWORD}
      - ConnectionStrings__Redis=redis:6379
      - FileStorage__Endpoint=minio:9000
      - AI__OllamaBaseUrl=http://ollama:11434
    depends_on: [postgres, redis, minio, ollama]

  worker:
    build: ./src/TechTaskReview.Web
    command: ["dotnet", "TechTaskReview.Web.dll", "--worker"]
    environment: # Same as api
    depends_on: [postgres, redis, minio, ollama, clamav]

  postgres:
    image: postgres:16-alpine
    volumes: ["pgdata:/var/lib/postgresql/data"]
    environment:
      POSTGRES_DB: techtask
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    volumes: ["miniodata:/data"]

  ollama:
    image: ollama/ollama:latest
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    volumes: ["ollamamodels:/root/.ollama"]

  clamav:
    image: clamav/clamav:latest
    volumes: ["clamavdb:/var/lib/clamav"]

volumes:
  pgdata:
  miniodata:
  ollamamodels:
  clamavdb:
```

---

## V2 Scope

### V2 Features

| Feature | Priority | Complexity |
|---------|----------|-----------|
| **Real-time updates via WebSocket/SSE** | P0 | Medium — Replace polling with SignalR. Push status updates as pipeline progresses. |
| **Email notifications** | P0 | Low — Send email when review completes. Use SMTP or SendGrid. |
| **Candidate self-service portal** | P1 | Medium — Candidates submit their own code via a unique link. Auth via one-time token. |
| **Bulk candidate import (CSV)** | P1 | Low — Parse CSV, create candidates in batch. |
| **Cloud AI provider integration** | P0 | Low — Enable OpenAI/Anthropic via config toggle. Already abstracted. |
| **Multi-model comparison** | P1 | Medium — Run same submission through 2 AI models, show diff. |
| **Final holistic AI pass** | P1 | Low — Send aggregated results back to AI for executive summary. |
| **Advanced comparison (radar charts)** | P1 | Low — Already have data; just add Recharts radar overlay. |
| **Mobile-responsive UI** | P2 | Medium — Tailwind responsive utilities + viewport testing. |
| **Kubernetes deployment** | P1 | Medium — Helm charts, HPA, PDB, resource quotas. |
| **CI/CD pipeline** | P0 | Medium — GitHub Actions: build → test → Docker push → deploy. |
| **Score calibration dashboard** | P2 | Medium — Baseline corpus management, drift tracking. |
| **Custom scoring templates** | P1 | Low — Allow admins to create new scoring configs from scratch. |
| **Reviewer role (read-only + comments)** | P1 | Low — Already in RBAC, just need comment model. |

### V2 Technical Improvements

| Improvement | Description |
|-------------|-------------|
| PostgreSQL read replicas | Route dashboard/ranking queries to replica |
| Redis Sentinel | High availability for cache and queue backing |
| Structured logging (Serilog → Seq/Loki) | Centralized log aggregation |
| OpenTelemetry traces | Distributed tracing through pipeline stages |
| Prometheus + Grafana | Metrics dashboards for SLO monitoring |
| Rate limiting per user (not just IP) | Prevent authenticated abuse |
| EF Core compiled queries | Pre-compile hot path queries for ~30% latency improvement |

---

## V3 / Enterprise Features

### Multi-Tenant Architecture

```
┌─────────────────────────────────────────────────────┐
│  Tenancy Strategy: Schema-per-tenant                │
│                                                      │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐    │
│  │ tenant_a   │  │ tenant_b   │  │ tenant_c   │    │
│  │ (schema)   │  │ (schema)   │  │ (schema)   │    │
│  │            │  │            │  │            │    │
│  │ candidates │  │ candidates │  │ candidates │    │
│  │ submissions│  │ submissions│  │ submissions│    │
│  │ reviews    │  │ reviews    │  │ reviews    │    │
│  └────────────┘  └────────────┘  └────────────┘    │
│                                                      │
│  Shared: users, tenants, billing (public schema)    │
└─────────────────────────────────────────────────────┘
```

**Why schema-per-tenant over row-level?**
- Stronger data isolation (compliance requirement for enterprise)
- Easier per-tenant backup/restore
- No risk of cross-tenant data leak via missing WHERE clause
- Trade-off: more complex migrations (must apply to all schemas)

### AI Fine-Tuning Possibility

```
Phase 1 (V1-V2): Use general-purpose code models (DeepSeek, GPT-4o-mini)
Phase 2 (V3):    Collect human feedback on AI reviews (thumbs up/down per category)
Phase 3 (V3+):   Fine-tune DeepSeek Coder on (code, human_review) pairs
                  Target: 20,000+ labeled examples before fine-tuning

Infrastructure needed:
- Human review interface (recruiter marks "agree/disagree" on each category)
- Training data export pipeline
- Fine-tuning infrastructure (single A100 GPU, ~4 hours per training run)
- A/B testing framework to compare fine-tuned vs base model
```

### On-Premise Deployment Model

```
Deliverable: Helm chart + offline container registry

Requirements:
- All containers pre-built and published to customer's registry
- Ollama models bundled (no internet dependency)
- ClamAV signatures bundled (with update mechanism)
- PostgreSQL and Redis included or BYODB
- Air-gapped deployment supported

Configuration:
- values.yaml with all environment-specific settings
- TLS certificate injection
- LDAP/AD integration for auth
- Customer-provided S3/MinIO endpoint
```

### Audit Compliance Readiness

| Standard | Current Status | Gap |
|----------|---------------|-----|
| SOC 2 Type II | Partial (audit logging exists) | Need formal access reviews, change management process |
| GDPR | Partial (PII in candidates table) | Need data deletion workflow, consent tracking, DPA |
| ISO 27001 | Not started | Need ISMS framework, risk register |
| HIPAA | N/A | Not applicable (no health data) |

**Data subject rights** (GDPR):
- Right to access: Export all candidate data as JSON
- Right to deletion: Cascade delete candidate + all submissions + reviews + audit entries
- Right to rectification: Update candidate PII
- Data retention: Configurable per tenant, automated cleanup

---

## Milestone Timeline (Feature Scope Only)

| Milestone | Scope |
|-----------|-------|
| **M1** | Backend skeleton: Clean Architecture, EF Core, migrations, auth |
| **M2** | File upload + extraction + malware scan pipeline |
| **M3** | Static analysis integration (ESLint + dotnet) |
| **M4** | AI review pipeline (Ollama, chunking, aggregation) |
| **M5** | Scoring computation + review persistence |
| **M6** | Frontend: dashboard, submission list, upload form |
| **M7** | Frontend: review detail, score charts, comparison |
| **M8** | PDF export, admin panel, scoring config editor |
| **M9** | Docker Compose deployment, end-to-end testing |
| **M10** | Hardening: rate limiting, backpressure, monitoring |

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Ollama quality insufficient for scoring | Medium | High | Provider abstraction allows quick switch to cloud AI. Keep budget for GPT-4o-mini fallback. |
| AI prompt injection manipulates scores | Medium | Medium | Structural JSON enforcement, score bounds in domain, anomaly detection (V2). |
| 100k submissions/month exceeds single Ollama | High | High | Horizontal scaling plan ready. Cloud overflow for peaks. Accept queue delays. |
| ClamAV misses novel malware | Low | High | Defense in depth: sandboxed extraction, no code execution, read-only Docker mounts. |
| PostgreSQL bottleneck at scale | Low | Medium | Read replicas, compiled queries, connection pooling. Well-indexed schema. |
| Model bias produces unfair scores | Medium | High | Calibration corpus, drift detection, confidence weighting, human review option. |
| Dependency on Hangfire OSS limitations | Low | Low | MassTransit migration path exists. Hangfire Pro available for advanced features. |
| GDPR compliance gaps | Medium | High | Start with data deletion workflow in V1. Full compliance audit in V2. |
