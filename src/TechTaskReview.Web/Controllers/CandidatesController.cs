using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTaskReview.Application.Candidates.Commands.CreateCandidate;
using TechTaskReview.Application.Candidates.Queries.GetCandidate;
using TechTaskReview.Application.Candidates.Queries.GetCandidates;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Web.Models;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CandidatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CandidateRole? role = null, [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCandidatesQuery(page, pageSize, role, search), ct);
        return Ok(new { items = result.Items, page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateQuery(id), ct);
        return result is not null ? Ok(ApiResponse<CandidateDetailDto>.Ok(result)) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Create([FromBody] CreateCandidateCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, ApiResponse<object>.Ok(new { id = result.Data }))
            : BadRequest(ApiResponse<object>.Fail(result.Errors.Select(e => new ApiError("VALIDATION_ERROR", e)).ToArray()));
    }
}
