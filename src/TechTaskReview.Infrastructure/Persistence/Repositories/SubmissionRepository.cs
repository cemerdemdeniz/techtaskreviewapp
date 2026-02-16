using Microsoft.EntityFrameworkCore;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.Persistence.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public SubmissionRepository(ApplicationDbContext context) => _context = context;

    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Review)
                .ThenInclude(r => r!.CategoryScores)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Submission>> GetByCandidateIdAsync(Guid candidateId, CancellationToken ct)
        => await _context.Submissions
            .Include(s => s.Review)
            .Where(s => s.CandidateId == candidateId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<PaginatedList<Submission>> GetPaginatedAsync(
        int page, int pageSize, SubmissionStatus? statusFilter, CancellationToken ct)
    {
        var query = _context.Submissions
            .Include(s => s.Review)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(s => s.Status == statusFilter.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedList<Submission>(items, totalCount, page, pageSize);
    }

    public void Add(Submission submission) => _context.Submissions.Add(submission);
    public void Update(Submission submission) => _context.Submissions.Update(submission);
}
