using Microsoft.EntityFrameworkCore;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Reviews;
using TechTaskReview.Domain.Aggregates.ScoringConfigs;
using TechTaskReview.Domain.Aggregates.Submissions;
using TechTaskReview.Domain.Aggregates.Users;
using TechTaskReview.Infrastructure.Persistence.Configurations;

namespace TechTaskReview.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
    public DbSet<CodeReview> CodeReviews => Set<CodeReview>();
    public DbSet<CategoryScore> CategoryScores => Set<CategoryScore>();
    public DbSet<ScoringConfig> ScoringConfigs => Set<ScoringConfig>();
    public DbSet<CategoryWeight> CategoryWeights => Set<CategoryWeight>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
