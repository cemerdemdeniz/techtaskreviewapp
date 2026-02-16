using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.ScoringConfigs;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class ScoringConfigConfiguration : IEntityTypeConfiguration<ScoringConfig>
{
    public void Configure(EntityTypeBuilder<ScoringConfig> builder)
    {
        builder.ToTable("scoring_configs");
        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Name).HasMaxLength(100).IsRequired();
        builder.Property(sc => sc.TargetRole).IsRequired();
        builder.Property(sc => sc.Version).HasDefaultValue(1);

        builder.HasMany(sc => sc.Weights)
            .WithOne()
            .HasForeignKey(w => w.ScoringConfigId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(sc => sc.Weights).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Only one default per role
        builder.HasIndex(sc => sc.TargetRole)
            .HasFilter("is_default = true")
            .IsUnique();
    }
}
