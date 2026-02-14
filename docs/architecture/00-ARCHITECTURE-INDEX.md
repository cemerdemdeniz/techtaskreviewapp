# TechTaskReview — Production Architecture Document

## Document Index

| # | Document | Contents |
|---|----------|----------|
| 01 | [System Overview](01-SYSTEM-OVERVIEW.md) | High-level architecture, component map, deployment topology |
| 02 | [Backend Architecture](02-BACKEND-ARCHITECTURE.md) | Clean Architecture layers, folder structure, patterns |
| 03 | [Domain Model](03-DOMAIN-MODEL.md) | Aggregates, entities, value objects, enumerations |
| 04 | [Database Schema](04-DATABASE-SCHEMA.md) | Tables, relationships, indexes, migrations strategy |
| 05 | [File Processing Pipeline](05-FILE-PROCESSING-PIPELINE.md) | Upload → extract → analyze → score pipeline |
| 06 | [AI Orchestration](06-AI-ORCHESTRATION.md) | Chunking, prompt engineering, aggregation, provider abstraction |
| 07 | [Scoring Model](07-SCORING-MODEL.md) | Weighted scoring, normalization, bias mitigation, versioning |
| 08 | [Frontend Architecture](08-FRONTEND-ARCHITECTURE.md) | React/TS structure, state, data fetching, components |
| 09 | [API Contracts](09-API-CONTRACTS.md) | Endpoint definitions, request/response schemas |
| 10 | [Security Model](10-SECURITY-MODEL.md) | Auth, RBAC, threat model, file validation |
| 11 | [Performance & Scaling](11-PERFORMANCE-SCALING.md) | Queue strategy, caching, horizontal scaling, cost |
| 12 | [Failure Recovery](12-FAILURE-RECOVERY.md) | Retry policies, circuit breakers, dead-letter queues |
| 13 | [Roadmap](13-ROADMAP.md) | MVP scope, V2 features, future scaling |

## Assumptions

1. **Single-tenant MVP** — one company's internal hiring platform. Multi-tenant discussed in roadmap.
2. **100k submissions/month** — ~3,300/day, ~230/hour sustained, 500 peak concurrent uploads.
3. **Average repo 25MB** — storage budget ~2.5TB/month raw, ~5TB with extracted artifacts.
4. **Free-tier AI initially** — Ollama with local models (CodeLlama, DeepSeek Coder) for MVP. OpenAI/Anthropic as paid upgrade path.
5. **Docker-based deployment** — single-host Docker Compose for MVP, Kubernetes-ready for scale.
6. **No real-time collaboration** — recruiters view results asynchronously after processing completes.
7. **English-only** UI and analysis for MVP.
