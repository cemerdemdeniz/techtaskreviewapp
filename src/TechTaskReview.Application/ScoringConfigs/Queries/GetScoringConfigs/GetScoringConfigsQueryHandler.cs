using MediatR;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Application.ScoringConfigs.Queries.GetScoringConfigs;

public class GetScoringConfigsQueryHandler : IRequestHandler<GetScoringConfigsQuery, List<ScoringConfigListDto>>
{
    private readonly IScoringConfigRepository _configs;

    public GetScoringConfigsQueryHandler(IScoringConfigRepository configs)
    {
        _configs = configs;
    }

    public async Task<List<ScoringConfigListDto>> Handle(GetScoringConfigsQuery request, CancellationToken ct)
    {
        var configs = await _configs.GetAllAsync(ct);
        return configs.Select(c => new ScoringConfigListDto(
            c.Id, c.Name, c.TargetRole.ToString(), c.IsDefault,
            c.Version, c.Weights.Count, c.CreatedAt
        )).ToList();
    }
}
