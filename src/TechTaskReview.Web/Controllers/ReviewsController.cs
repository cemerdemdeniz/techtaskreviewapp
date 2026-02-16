using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Reviews.Queries.GetReview;
using TechTaskReview.Application.Reviews.Queries.GetReviewComparison;
using TechTaskReview.Web.Models;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPdfExportService _pdfExport;
    private readonly ICodeReviewRepository _reviews;
    private readonly ICandidateRepository _candidates;
    private readonly ISubmissionRepository _submissions;

    public ReviewsController(IMediator mediator, IPdfExportService pdfExport,
        ICodeReviewRepository reviews, ICandidateRepository candidates, ISubmissionRepository submissions)
    {
        _mediator = mediator;
        _pdfExport = pdfExport;
        _reviews = reviews;
        _candidates = candidates;
        _submissions = submissions;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReviewQuery(id), ct);
        return result is not null ? Ok(ApiResponse<ReviewDetailDto>.Ok(result)) : NotFound();
    }

    [HttpGet("compare")]
    public async Task<IActionResult> Compare(
        [FromQuery] Guid candidateA, [FromQuery] Guid candidateB, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReviewComparisonQuery(candidateA, candidateB), ct);
        return result is not null ? Ok(ApiResponse<ComparisonResultDto>.Ok(result)) : NotFound();
    }

    [HttpPost("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var review = await _reviews.GetByIdAsync(id, ct);
        if (review is null) return NotFound();

        var submission = await _submissions.GetByIdAsync(review.SubmissionId, ct);
        var candidate = submission is not null ? await _candidates.GetByIdAsync(submission.CandidateId, ct) : null;

        var pdfBytes = await _pdfExport.ExportReviewAsync(review, candidate?.FullName ?? "Unknown", ct);
        return File(pdfBytes, "application/pdf", $"review-{id}.pdf");
    }
}
