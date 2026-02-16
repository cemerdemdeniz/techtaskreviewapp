namespace TechTaskReview.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string[] Errors { get; }

    private Result(bool isSuccess, T? data, string[] errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        Errors = errors;
    }

    public static Result<T> Success(T data) => new(true, data, []);
    public static Result<T> Failure(params string[] errors) => new(false, default, errors);
}
