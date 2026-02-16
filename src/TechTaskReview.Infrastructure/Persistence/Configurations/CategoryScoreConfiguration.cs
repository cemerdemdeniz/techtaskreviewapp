using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class CategoryScoreConfiguration : IEntityTypeConfiguration<CategoryScore>
{
    public void Configure(EntityTypeBuilder<CategoryScore> builder)
    {
        builder.ToTable("category_scores");
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.Score).HasPrecision(4, 2);
        builder.Property(cs => cs.Justification).IsRequired();
        builder.Property(cs => cs.CriticalIssues).HasColumnType("jsonb");
        builder.Property(cs => cs.RefactorSuggestions).HasColumnType("jsonb");
        builder.Property(cs => cs.SeniorLevelImprovementPlan).IsRequired();
        builder.Property(cs => cs.Confidence).HasPrecision(3, 2);

        builder.HasIndex(cs => cs.CodeReviewId);
        builder.HasIndex(cs => new { cs.CodeReviewId, cs.Category }).IsUnique();
    }
}
