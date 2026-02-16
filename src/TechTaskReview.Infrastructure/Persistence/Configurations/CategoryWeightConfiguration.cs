using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.ScoringConfigs;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class CategoryWeightConfiguration : IEntityTypeConfiguration<CategoryWeight>
{
    public void Configure(EntityTypeBuilder<CategoryWeight> builder)
    {
        builder.ToTable("category_weights");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Weight).HasPrecision(5, 4);
        builder.Property(w => w.MinimumPassingScore).HasPrecision(4, 2);

        builder.HasIndex(w => new { w.ScoringConfigId, w.Category }).IsUnique();
    }
}
