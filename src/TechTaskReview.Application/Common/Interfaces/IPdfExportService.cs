using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Application.Common.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> ExportReviewAsync(CodeReview review, string candidateName, CancellationToken ct);
}
