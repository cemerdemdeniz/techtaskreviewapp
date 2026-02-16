using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.Reviews;
using TechTaskReview.Domain.Common;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Domain.Aggregates.ScoringConfigs;

public class ScoringConfig : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public CandidateRole TargetRole { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }
    public string? Description { get; private set; }

    private readonly List<CategoryWeight> _weights = [];
    public IReadOnlyList<CategoryWeight> Weights => _weights.AsReadOnly();

    private ScoringConfig() { }

    public static ScoringConfig Create(string name, CandidateRole targetRole, string? description = null)
    {
        return new ScoringConfig
        {
            Name = name,
            TargetRole = targetRole,
            Version = 1,
            Description = description
        };
    }

    public void AddWeight(ScoreCategory category, decimal weight, decimal? minimumPassingScore = null)
    {
        if (weight < 0 || weight > 1)
            throw new DomainException($"Weight must be between 0 and 1. Got: {weight}");

        _weights.Add(new CategoryWeight
        {
            ScoringConfigId = Id,
            Category = category,
            Weight = weight,
            MinimumPassingScore = minimumPassingScore
        });
    }

    public void UpdateWeights(string name, List<(ScoreCategory Category, decimal Weight, decimal? MinimumPassingScore)> weights)
    {
        Name = name;
        _weights.Clear();
        foreach (var (category, weight, minimumPassingScore) in weights)
        {
            AddWeight(category, weight, minimumPassingScore);
        }
        Version++;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public decimal ComputeWeightedScore(IReadOnlyList<CategoryScore> scores)
    {
        decimal weightedSum = 0;
        decimal totalWeight = 0;

        foreach (var weight in _weights)
        {
            var score = scores.FirstOrDefault(s => s.Category == weight.Category);
            if (score is not null)
            {
                weightedSum += score.Score * weight.Weight;
                totalWeight += weight.Weight;
            }
        }

        return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0;
    }

    public List<(ScoreCategory Category, decimal Score)> GetFailingCategories(IReadOnlyList<CategoryScore> scores)
    {
        return _weights
            .Where(w => w.MinimumPassingScore.HasValue)
            .Select(w => (w.Category, Score: scores.FirstOrDefault(s => s.Category == w.Category)?.Score ?? 0))
            .Where(x => x.Score < _weights.First(w => w.Category == x.Category).MinimumPassingScore!.Value)
            .ToList();
    }
}
