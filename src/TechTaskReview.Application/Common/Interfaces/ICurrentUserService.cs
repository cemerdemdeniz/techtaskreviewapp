namespace TechTaskReview.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserRole { get; }
    bool IsAuthenticated { get; }
}
