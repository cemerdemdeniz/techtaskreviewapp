using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Aggregates.Users;

public class User : AggregateRoot
{
    public string Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    public static User Create(string email, string fullName, string passwordHash, UserRole role)
    {
        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
}
