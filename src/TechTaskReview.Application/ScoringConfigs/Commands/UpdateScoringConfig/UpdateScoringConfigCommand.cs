using MediatR;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Application.ScoringConfigs.Commands.UpdateScoringConfig;

public record UpdateScoringConfigCommand : IRequest<Result<Guid>>
{
    public Guid Id { get; set; }
    public string Name { get; init; } = null!;
    public List<WeightUpdateDto> Weights { get; init; } = [];
}

public record WeightUpdateDto(ScoreCategory Category, decimal Weight, decimal? MinimumPassingScore);
