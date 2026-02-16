using MediatR;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Application.ScoringConfigs.Commands.UpdateScoringConfig;

public class UpdateScoringConfigCommandHandler : IRequestHandler<UpdateScoringConfigCommand, Result<Guid>>
{
    private readonly IScoringConfigRepository _configs;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateScoringConfigCommandHandler(IScoringConfigRepository configs, IUnitOfWork unitOfWork)
    {
        _configs = configs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(UpdateScoringConfigCommand request, CancellationToken ct)
    {
        var config = await _configs.GetByIdAsync(request.Id, ct);
        if (config is null)
            return Result<Guid>.Failure("Scoring config not found.");

        config.UpdateWeights(request.Name, request.Weights.Select(w =>
            (w.Category, w.Weight, w.MinimumPassingScore)).ToList());

        _configs.Update(config);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(config.Id);
    }
}
