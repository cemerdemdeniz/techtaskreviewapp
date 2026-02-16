using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class CodeReviewConfiguration : IEntityTypeConfiguration<CodeReview>
{
    public void Configure(EntityTypeBuilder<CodeReview> builder)
    {
        builder.ToTable("code_reviews");
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.Version, v =>
        {
            v.Property(p => p.Major).HasColumnName("version_major");
            v.Property(p => p.Minor).HasColumnName("version_minor");
            v.Property(p => p.ScoringModelVersion).HasColumnName("scoring_model_version").HasMaxLength(20);
        });

        builder.Property(r => r.AIProviderName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.AIModelName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.WeightedTotalScore).HasPrecision(5, 2);
        builder.Property(r => r.RawResponseJson).HasColumnType("jsonb");
        builder.Property(r => r.StaticAnalysisResultsJson).HasColumnType("jsonb");

        builder.HasMany(r => r.CategoryScores)
            .WithOne()
            .HasForeignKey(cs => cs.CodeReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.CategoryScores).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.SubmissionId).IsUnique();
        builder.HasIndex(r => r.WeightedTotalScore);
    }
}
