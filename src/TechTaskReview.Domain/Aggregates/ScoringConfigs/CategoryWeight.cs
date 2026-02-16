using TechTaskReview.Domain.Aggregates.Reviews;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Aggregates.ScoringConfigs;

public class CategoryWeight : Entity
{
    public Guid ScoringConfigId { get; set; }
    public ScoreCategory Category { get; set; }
    public decimal Weight { get; set; }           // 0.0 - 1.0 (relative weight)
    public decimal? MinimumPassingScore { get; set; }  // Optional floor (e.g., Security must be >= 5)
}
