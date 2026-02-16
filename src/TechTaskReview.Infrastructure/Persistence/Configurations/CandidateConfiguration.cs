using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Role).IsRequired();
        builder.Property(c => c.Position).HasMaxLength(200);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasIndex(c => c.Role);

        builder.HasMany(c => c.Submissions)
            .WithOne()
            .HasForeignKey(s => s.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Access private field for collection
        builder.Navigation(c => c.Submissions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
