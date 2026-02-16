using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.ToTable("submission_files");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RelativePath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Language).HasMaxLength(50).IsRequired();
        builder.HasIndex(f => f.SubmissionId);
    }
}
