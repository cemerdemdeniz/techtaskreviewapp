using Microsoft.EntityFrameworkCore;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Reviews;

namespace TechTaskReview.Infrastructure.Persistence.Repositories;

public class CodeReviewRepository : ICodeReviewRepository
{
    private readonly ApplicationDbContext _context;

    public CodeReviewRepository(ApplicationDbContext context) => _context = context;

    public async Task<CodeReview?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.CodeReviews
            .Include(r => r.CategoryScores)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<CodeReview?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct)
        => await _context.CodeReviews
            .Include(r => r.CategoryScores)
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId, ct);

    public void Add(CodeReview review) => _context.CodeReviews.Add(review);
    public void Update(CodeReview review) => _context.CodeReviews.Update(review);
}
