using System.Net;
using System.Text.Json;
using FluentValidation;
using TechTaskReview.Domain.Exceptions;

namespace TechTaskReview.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                validationEx.Errors.Select(e => new { code = "VALIDATION_ERROR", message = e.ErrorMessage, field = e.PropertyName })),
            InvalidSubmissionException submissionEx => (
                HttpStatusCode.UnprocessableEntity,
                new[] { new { code = "INVALID_SUBMISSION", message = submissionEx.Message, field = (string?)null } }.AsEnumerable()),
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new[] { new { code = "DOMAIN_ERROR", message = domainEx.Message, field = (string?)null } }.AsEnumerable()),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new[] { new { code = "NOT_FOUND", message = "Resource not found.", field = (string?)null } }.AsEnumerable()),
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                new[] { new { code = "FORBIDDEN", message = "Access denied.", field = (string?)null } }.AsEnumerable()),
            _ => (
                HttpStatusCode.InternalServerError,
                new[] { new { code = "INTERNAL_ERROR", message = "An unexpected error occurred.", field = (string?)null } }.AsEnumerable())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new { success = false, errors });
    }
}
