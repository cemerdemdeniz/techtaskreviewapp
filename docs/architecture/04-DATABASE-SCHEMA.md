# 04 — Database Schema

## Entity-Relationship Diagram

```
┌──────────────────┐       ┌──────────────────────┐       ┌──────────────────────┐
│     users         │       │    candidates         │       │  scoring_configs     │
├──────────────────┤       ├──────────────────────┤       ├──────────────────────┤
│ id          UUID PK      │ id          UUID  PK  │       │ id         UUID  PK  │
│ email       VARCHAR(256) │ full_name   VARCHAR(200)      │ name       VARCHAR(100)
│ full_name   VARCHAR(200) │ email       VARCHAR(256)      │ target_role SMALLINT │
│ password_hash TEXT       │ role        SMALLINT  │       │ is_default  BOOLEAN  │
│ role        SMALLINT     │ position    VARCHAR(200)      │ version     INT      │
│ is_active   BOOLEAN      │ notes       TEXT      │       │ description TEXT     │
│ last_login_at TIMESTAMPTZ│ created_at  TIMESTAMPTZ       │ created_at TIMESTAMPTZ
│ created_at  TIMESTAMPTZ  │ updated_at  TIMESTAMPTZ       │ updated_at TIMESTAMPTZ
│ updated_at  TIMESTAMPTZ  └──────────┬───────────┘       └──────────┬───────────┘
└──────────────────┘                  │ 1:N                           │ 1:N
                                      ▼                               ▼
┌─────────────────────────────────────────────────┐    ┌──────────────────────────┐
│               submissions                        │    │   category_weights       │
├─────────────────────────────────────────────────┤    ├──────────────────────────┤
│ id                 UUID  PK                      │    │ id           UUID  PK    │
│ candidate_id       UUID  FK → candidates.id      │    │ scoring_config_id UUID FK│
│ storage_path       VARCHAR(500)                  │    │ category     SMALLINT    │
│ original_file_name VARCHAR(256)                  │    │ weight       DECIMAL(5,4)│
│ file_size_bytes    BIGINT                        │    │ min_passing  DECIMAL(4,2)│
│ source             SMALLINT  (1=Zip, 2=Git)      │    │ created_at   TIMESTAMPTZ │
│ git_url            VARCHAR(500) NULL             │    └──────────────────────────┘
│ git_branch         VARCHAR(200) NULL             │
│ status             SMALLINT                      │
│ failure_reason     TEXT NULL                     │
│ primary_language   VARCHAR(50) NULL              │
│ framework          VARCHAR(50) NULL              │
│ build_files        TEXT[] NULL                   │    (PostgreSQL array)
│ is_monorepo        BOOLEAN NULL                  │
│ extracted_path     VARCHAR(500) NULL             │
│ total_files        INT NULL                      │
│ total_lines_of_code INT NULL                     │
│ processing_started_at  TIMESTAMPTZ NULL          │
│ processing_completed_at TIMESTAMPTZ NULL         │
│ created_at         TIMESTAMPTZ                   │
│ updated_at         TIMESTAMPTZ NULL              │
└────────────────────────┬────────────────────────┘
                         │ 1:N                1:1
                         ▼                     ▼
┌────────────────────────────┐    ┌──────────────────────────────────────┐
│     submission_files       │    │          code_reviews                 │
├────────────────────────────┤    ├──────────────────────────────────────┤
│ id            UUID  PK     │    │ id                UUID  PK           │
│ submission_id UUID  FK     │    │ submission_id     UUID  FK UNIQUE    │
│ relative_path VARCHAR(500) │    │ version_major     INT               │
│ language      VARCHAR(50)  │    │ version_minor     INT               │
│ line_count    INT          │    │ scoring_model_ver VARCHAR(20)        │
│ size_bytes    BIGINT       │    │ ai_provider_name  VARCHAR(50)        │
│ is_test_file  BOOLEAN      │    │ ai_model_name     VARCHAR(100)       │
│ is_config_file BOOLEAN     │    │ status            SMALLINT           │
│ created_at    TIMESTAMPTZ  │    │ weighted_total_score DECIMAL(5,2)    │
└────────────────────────────┘    │ summary           TEXT NULL          │
                                  │ raw_response_json  JSONB NULL        │
                                  │ static_analysis_json JSONB NULL      │
                                  │ chunks_processed   INT               │
                                  │ chunks_failed      INT               │
                                  │ processing_duration INTERVAL          │
                                  │ scoring_config_id  UUID FK            │
                                  │ created_at         TIMESTAMPTZ        │
                                  │ updated_at         TIMESTAMPTZ NULL   │
                                  └──────────────────┬───────────────────┘
                                                     │ 1:N
                                                     ▼
                                  ┌──────────────────────────────────────┐
                                  │         category_scores              │
                                  ├──────────────────────────────────────┤
                                  │ id                UUID  PK           │
                                  │ code_review_id    UUID  FK           │
                                  │ category          SMALLINT           │
                                  │ score             DECIMAL(4,2)       │
                                  │ justification     TEXT               │
                                  │ critical_issues   JSONB              │
                                  │ refactor_suggestions JSONB           │
                                  │ senior_improvement_plan TEXT         │
                                  │ confidence        DECIMAL(3,2)       │
                                  │ created_at        TIMESTAMPTZ        │
                                  └──────────────────────────────────────┘

┌──────────────────────────────────────┐
│          audit_logs                   │
├──────────────────────────────────────┤
│ id             BIGSERIAL PK          │   ← Not UUID for perf (append-only, high volume)
│ timestamp      TIMESTAMPTZ           │
│ user_id        VARCHAR(50)           │
│ action         VARCHAR(100)          │
│ entity_type    VARCHAR(100)          │
│ entity_id      VARCHAR(50) NULL      │
│ old_values     JSONB NULL            │
│ new_values     JSONB NULL            │
│ ip_address     INET NULL             │
└──────────────────────────────────────┘
```

## SQL Schema

```sql
-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- For text search on candidate names

-- Users
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email           VARCHAR(256) NOT NULL,
    full_name       VARCHAR(200) NOT NULL,
    password_hash   TEXT NOT NULL,
    role            SMALLINT NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at   TIMESTAMPTZ NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NULL,
    CONSTRAINT uq_users_email UNIQUE (email)
);

CREATE INDEX ix_users_email ON users (email);

-- Candidates
CREATE TABLE candidates (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    full_name       VARCHAR(200) NOT NULL,
    email           VARCHAR(256) NOT NULL,
    role            SMALLINT NOT NULL,
    position        VARCHAR(200) NULL,
    notes           TEXT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NULL,
    CONSTRAINT uq_candidates_email UNIQUE (email)
);

CREATE INDEX ix_candidates_email ON candidates (email);
CREATE INDEX ix_candidates_role ON candidates (role);
CREATE INDEX ix_candidates_name_trgm ON candidates USING gin (full_name gin_trgm_ops);

-- Submissions
CREATE TABLE submissions (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id            UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    storage_path            VARCHAR(500) NOT NULL,
    original_file_name      VARCHAR(256) NOT NULL,
    file_size_bytes         BIGINT NOT NULL DEFAULT 0,
    source                  SMALLINT NOT NULL,
    git_url                 VARCHAR(500) NULL,
    git_branch              VARCHAR(200) NULL,
    status                  SMALLINT NOT NULL DEFAULT 1,
    failure_reason          TEXT NULL,
    primary_language        VARCHAR(50) NULL,
    framework               VARCHAR(50) NULL,
    build_files             TEXT[] NULL,
    is_monorepo             BOOLEAN NULL,
    extracted_path          VARCHAR(500) NULL,
    total_files             INT NULL,
    total_lines_of_code     INT NULL,
    processing_started_at   TIMESTAMPTZ NULL,
    processing_completed_at TIMESTAMPTZ NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NULL
);

CREATE INDEX ix_submissions_candidate_id ON submissions (candidate_id);
CREATE INDEX ix_submissions_status ON submissions (status);
CREATE INDEX ix_submissions_created_at ON submissions (created_at DESC);
-- Composite for dashboard queries: "all completed submissions for role X, ordered by date"
CREATE INDEX ix_submissions_status_created ON submissions (status, created_at DESC)
    WHERE status IN (7, 8); -- Completed or Failed

-- Submission Files
CREATE TABLE submission_files (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    submission_id   UUID NOT NULL REFERENCES submissions(id) ON DELETE CASCADE,
    relative_path   VARCHAR(500) NOT NULL,
    language        VARCHAR(50) NOT NULL,
    line_count      INT NOT NULL,
    size_bytes      BIGINT NOT NULL,
    is_test_file    BOOLEAN NOT NULL DEFAULT FALSE,
    is_config_file  BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_submission_files_submission_id ON submission_files (submission_id);

-- Scoring Configs
CREATE TABLE scoring_configs (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(100) NOT NULL,
    target_role     SMALLINT NOT NULL,
    is_default      BOOLEAN NOT NULL DEFAULT FALSE,
    version         INT NOT NULL DEFAULT 1,
    description     TEXT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NULL
);

-- Partial unique index: only one default per role
CREATE UNIQUE INDEX uq_scoring_configs_default_per_role
    ON scoring_configs (target_role) WHERE is_default = TRUE;

-- Category Weights
CREATE TABLE category_weights (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    scoring_config_id   UUID NOT NULL REFERENCES scoring_configs(id) ON DELETE CASCADE,
    category            SMALLINT NOT NULL,
    weight              DECIMAL(5,4) NOT NULL,
    min_passing_score   DECIMAL(4,2) NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_weight_per_category UNIQUE (scoring_config_id, category),
    CONSTRAINT chk_weight_range CHECK (weight >= 0 AND weight <= 1),
    CONSTRAINT chk_min_passing CHECK (min_passing_score IS NULL OR (min_passing_score >= 0 AND min_passing_score <= 10))
);

-- Code Reviews
CREATE TABLE code_reviews (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    submission_id           UUID NOT NULL REFERENCES submissions(id) ON DELETE CASCADE,
    version_major           INT NOT NULL,
    version_minor           INT NOT NULL,
    scoring_model_version   VARCHAR(20) NOT NULL,
    ai_provider_name        VARCHAR(50) NOT NULL,
    ai_model_name           VARCHAR(100) NOT NULL,
    status                  SMALLINT NOT NULL DEFAULT 1,
    weighted_total_score    DECIMAL(5,2) NOT NULL DEFAULT 0,
    summary                 TEXT NULL,
    raw_response_json       JSONB NULL,
    static_analysis_json    JSONB NULL,
    chunks_processed        INT NOT NULL DEFAULT 0,
    chunks_failed           INT NOT NULL DEFAULT 0,
    processing_duration     INTERVAL NOT NULL DEFAULT '0 seconds',
    scoring_config_id       UUID NOT NULL REFERENCES scoring_configs(id),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NULL,
    CONSTRAINT uq_review_per_submission UNIQUE (submission_id)
);

CREATE INDEX ix_code_reviews_submission_id ON code_reviews (submission_id);
CREATE INDEX ix_code_reviews_score ON code_reviews (weighted_total_score DESC);
-- GIN index on JSONB for querying raw AI responses
CREATE INDEX ix_code_reviews_raw_json ON code_reviews USING gin (raw_response_json);

-- Category Scores
CREATE TABLE category_scores (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code_review_id              UUID NOT NULL REFERENCES code_reviews(id) ON DELETE CASCADE,
    category                    SMALLINT NOT NULL,
    score                       DECIMAL(4,2) NOT NULL,
    justification               TEXT NOT NULL,
    critical_issues             JSONB NOT NULL DEFAULT '[]',
    refactor_suggestions        JSONB NOT NULL DEFAULT '[]',
    senior_improvement_plan     TEXT NOT NULL,
    confidence                  DECIMAL(3,2) NOT NULL DEFAULT 0.5,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_score_per_category UNIQUE (code_review_id, category),
    CONSTRAINT chk_score_range CHECK (score >= 0 AND score <= 10),
    CONSTRAINT chk_confidence_range CHECK (confidence >= 0 AND confidence <= 1)
);

CREATE INDEX ix_category_scores_review_id ON category_scores (code_review_id);
CREATE INDEX ix_category_scores_category ON category_scores (category);

-- Audit Logs (append-only, high-volume)
CREATE TABLE audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id         VARCHAR(50) NOT NULL,
    action          VARCHAR(100) NOT NULL,
    entity_type     VARCHAR(100) NOT NULL,
    entity_id       VARCHAR(50) NULL,
    old_values      JSONB NULL,
    new_values      JSONB NULL,
    ip_address      INET NULL
);

-- Partition by month for audit log performance at scale
-- (implement when audit_logs > 10M rows; for MVP, simple table suffices)
CREATE INDEX ix_audit_logs_timestamp ON audit_logs (timestamp DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_type, entity_id);
CREATE INDEX ix_audit_logs_user ON audit_logs (user_id);
```

## Migration Strategy

- **EF Core Migrations** for schema versioning
- Naming convention: `YYYYMMDD_HHMMSS_DescriptiveName.cs`
- All migrations are idempotent (`IF NOT EXISTS` guards in raw SQL)
- Seed data for:
  - Default admin user
  - Default scoring configs (one for Frontend, one for Backend)
  - Default category weights

## Indexing Rationale

| Index | Query Pattern | Expected Volume |
|-------|--------------|-----------------|
| `ix_submissions_status` | Dashboard: "all pending submissions" | ~3k active at any time |
| `ix_submissions_status_created` (partial) | Dashboard: "completed submissions sorted by date" | ~100k/month |
| `ix_code_reviews_score` | Rankings: "top candidates by score" | ~100k reviews |
| `ix_candidates_name_trgm` | Search: "find candidate by partial name" | ~50k candidates/year |
| `ix_audit_logs_timestamp` | Compliance: "audit trail for date range" | ~1M/month |

## Data Retention Policy

| Data Type | Retention | Rationale |
|-----------|-----------|-----------|
| Submissions (metadata) | 2 years | Legal/compliance |
| Extracted repos (files) | 30 days after completion | Storage cost (~2.5TB/mo raw) |
| Raw AI responses (JSONB) | 1 year | Debugging, model comparison |
| Audit logs | 3 years | Compliance |
| Generated PDFs | 90 days | Re-generatable on demand |

The `CleanupJob` (daily Hangfire recurring job) purges:
1. Extracted repos older than 30 days (from MinIO/filesystem)
2. PDFs older than 90 days (from MinIO)
3. Audit logs older than 3 years (from PostgreSQL via batch DELETE)
