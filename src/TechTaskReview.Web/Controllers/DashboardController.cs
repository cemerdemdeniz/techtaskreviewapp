using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTaskReview.Application.Dashboard.Queries.GetDashboardSummary;
using TechTaskReview.Web.Models;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
    }
}
