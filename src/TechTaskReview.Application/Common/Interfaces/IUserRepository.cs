using TechTaskReview.Domain.Aggregates.Users;

namespace TechTaskReview.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    void Add(User user);
    void Update(User user);
}
