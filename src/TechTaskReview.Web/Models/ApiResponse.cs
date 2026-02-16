namespace TechTaskReview.Web.Models;

public record ApiResponse<T>(bool Success, T? Data, object[]? Errors = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(params object[] errors) => new(false, default, errors);
}

public record ApiError(string Code, string Message, string? Field = null);
