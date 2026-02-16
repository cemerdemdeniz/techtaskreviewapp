using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTaskReview.Application.ScoringConfigs.Commands.UpdateScoringConfig;
using TechTaskReview.Application.ScoringConfigs.Queries.GetScoringConfigs;
using TechTaskReview.Web.Models;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/scoring-configs")]
[Authorize]
public class ScoringConfigsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScoringConfigsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetScoringConfigsQuery(), ct);
        return Ok(new { items = result });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScoringConfigCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok(new { id = result.Data }))
            : BadRequest(ApiResponse<object>.Fail(result.Errors.Select(e => new ApiError("ERROR", e)).ToArray()));
    }
}
