using Microsoft.EntityFrameworkCore;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Candidates;
using TechTaskReview.Domain.Aggregates.ScoringConfigs;

namespace TechTaskReview.Infrastructure.Persistence.Repositories;

public class ScoringConfigRepository : IScoringConfigRepository
{
    private readonly ApplicationDbContext _context;

    public ScoringConfigRepository(ApplicationDbContext context) => _context = context;

    public async Task<ScoringConfig?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.ScoringConfigs
            .Include(sc => sc.Weights)
            .FirstOrDefaultAsync(sc => sc.Id == id, ct);

    public async Task<ScoringConfig?> GetDefaultForRoleAsync(CandidateRole role, CancellationToken ct)
        => await _context.ScoringConfigs
            .Include(sc => sc.Weights)
            .FirstOrDefaultAsync(sc => sc.TargetRole == role && sc.IsDefault, ct);

    public async Task<IReadOnlyList<ScoringConfig>> GetAllAsync(CancellationToken ct)
        => await _context.ScoringConfigs
            .Include(sc => sc.Weights)
            .OrderBy(sc => sc.TargetRole)
            .ThenBy(sc => sc.Name)
            .ToListAsync(ct);

    public void Add(ScoringConfig config) => _context.ScoringConfigs.Add(config);
    public void Update(ScoringConfig config) => _context.ScoringConfigs.Update(config);
}
