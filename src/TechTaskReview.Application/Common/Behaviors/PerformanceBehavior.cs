using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TechTaskReview.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer = new();

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _timer.Start();
        var response = await next();
        _timer.Stop();

        var elapsed = _timer.ElapsedMilliseconds;
        if (elapsed > 500)
        {
            _logger.LogWarning("Long running request: {RequestName} ({ElapsedMs}ms)", typeof(TRequest).Name, elapsed);
        }

        return response;
    }
}
