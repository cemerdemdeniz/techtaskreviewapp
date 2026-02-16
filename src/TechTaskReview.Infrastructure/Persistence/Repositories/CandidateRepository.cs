using Microsoft.EntityFrameworkCore;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Common.Models;
using TechTaskReview.Domain.Aggregates.Candidates;

namespace TechTaskReview.Infrastructure.Persistence.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly ApplicationDbContext _context;

    public CandidateRepository(ApplicationDbContext context) => _context = context;

    public async Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Candidates
            .Include(c => c.Submissions)
                .ThenInclude(s => s.Review)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Candidate?> GetByEmailAsync(string email, CancellationToken ct)
        => await _context.Candidates.FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant(), ct);

    public async Task<PaginatedList<Candidate>> GetPaginatedAsync(
        int page, int pageSize, CandidateRole? roleFilter, string? search, CancellationToken ct)
    {
        var query = _context.Candidates
            .Include(c => c.Submissions)
                .ThenInclude(s => s.Review)
            .AsQueryable();

        if (roleFilter.HasValue)
            query = query.Where(c => c.Role == roleFilter.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, $"%{search}%") ||
                EF.Functions.ILike(c.Email, $"%{search}%"));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedList<Candidate>(items, totalCount, page, pageSize);
    }

    public void Add(Candidate candidate) => _context.Candidates.Add(candidate);
    public void Update(Candidate candidate) => _context.Candidates.Update(candidate);
    public void Delete(Candidate candidate) => _context.Candidates.Remove(candidate);
}
