using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StoragePath).HasMaxLength(500);
        builder.Property(s => s.OriginalFileName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.FileSizeBytes).HasDefaultValue(0L);
        builder.Property(s => s.Source).IsRequired();
        builder.Property(s => s.GitUrl).HasMaxLength(500);
        builder.Property(s => s.GitBranch).HasMaxLength(200);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.ExtractedPath).HasMaxLength(500);

        builder.OwnsOne(s => s.DetectedProjectType, pt =>
        {
            pt.Property(p => p.PrimaryLanguage).HasColumnName("primary_language").HasMaxLength(50);
            pt.Property(p => p.Framework).HasColumnName("framework").HasMaxLength(50);
            pt.Property(p => p.BuildFiles).HasColumnName("build_files");
            pt.Property(p => p.IsMonorepo).HasColumnName("is_monorepo");
        });

        builder.HasMany(s => s.Files)
            .WithOne()
            .HasForeignKey(f => f.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Files).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(s => s.Review)
            .WithOne()
            .HasForeignKey<CodeReview>(r => r.SubmissionId);

        builder.HasIndex(s => s.CandidateId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.Status, s.CreatedAt });
    }
}
