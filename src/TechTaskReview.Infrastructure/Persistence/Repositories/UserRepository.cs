using Microsoft.EntityFrameworkCore;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Aggregates.Users;

namespace TechTaskReview.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
        => await _context.Users.OrderBy(u => u.FullName).ToListAsync(ct);

    public void Add(User user) => _context.Users.Add(user);
    public void Update(User user) => _context.Users.Update(user);
}
