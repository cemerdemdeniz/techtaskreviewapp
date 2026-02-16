using MediatR;

namespace TechTaskReview.Application.ScoringConfigs.Queries.GetScoringConfigs;

public record GetScoringConfigsQuery : IRequest<List<ScoringConfigListDto>>;

public record ScoringConfigListDto(
    Guid Id,
    string Name,
    string TargetRole,
    bool IsDefault,
    int Version,
    int WeightCount,
    DateTime CreatedAt);
