using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechTaskReview.Application.Submissions.Commands.CreateGitSubmission;
using TechTaskReview.Application.Submissions.Commands.CreateSubmission;
using TechTaskReview.Application.Submissions.Commands.RetrySubmission;
using TechTaskReview.Application.Submissions.Queries.GetSubmission;
using TechTaskReview.Application.Submissions.Queries.GetSubmissions;
using TechTaskReview.Application.Submissions.Queries.GetSubmissionStatus;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Web.Models;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubmissionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSubmissionsQuery(page, pageSize), ct);
        return Ok(new { items = result.Items, page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubmissionQuery(id), ct);
        return result is not null ? Ok(ApiResponse<SubmissionDetailDto>.Ok(result)) : NotFound();
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubmissionStatusQuery(id), ct);
        return result is not null ? Ok(ApiResponse<SubmissionStatusDto>.Ok(result)) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter")]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] Guid candidateId,
        [FromForm] CandidateRole role,
        IFormFile file,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var command = new CreateSubmissionCommand(candidateId, stream, file.FileName, file.Length, role);
        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetStatus), new { id = result.Data }, ApiResponse<object>.Ok(new { id = result.Data, status = "Uploaded", statusUrl = $"/api/v1/submissions/{result.Data}/status" }))
            : BadRequest(ApiResponse<object>.Fail(result.Errors.Select(e => new ApiError("VALIDATION_ERROR", e)).ToArray()));
    }

    [HttpPost("git")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> UploadGit([FromBody] CreateGitSubmissionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetStatus), new { id = result.Data }, ApiResponse<object>.Ok(new { id = result.Data, status = "Uploaded" }))
            : BadRequest(ApiResponse<object>.Fail(result.Errors.Select(e => new ApiError("VALIDATION_ERROR", e)).ToArray()));
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RetrySubmissionCommand(id), ct);
        return result.IsSuccess
            ? AcceptedAtAction(nameof(GetStatus), new { id = result.Data }, ApiResponse<object>.Ok(new { id = result.Data, status = "Uploaded" }))
            : BadRequest(ApiResponse<object>.Fail(result.Errors.Select(e => new ApiError("VALIDATION_ERROR", e)).ToArray()));
    }
}
