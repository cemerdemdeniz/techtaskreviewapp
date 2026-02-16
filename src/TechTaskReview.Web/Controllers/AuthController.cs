using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Web.Models;
using TechTaskReview.Web.Services;

namespace TechTaskReview.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly JwtTokenService _jwtService;

    public AuthController(IUserRepository users, JwtTokenService jwtService)
    {
        _users = users;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { success = false, errors = new[] { new ApiError("INVALID_CREDENTIALS", "Invalid email or password.") } });

        if (!user.IsActive)
            return Unauthorized(new { success = false, errors = new[] { new ApiError("ACCOUNT_DISABLED", "Account is disabled.") } });

        user.RecordLogin();
        var token = _jwtService.GenerateToken(user);

        return Ok(ApiResponse<object>.Ok(new
        {
            accessToken = token,
            expiresAt = DateTime.UtcNow.AddMinutes(30),
            user = new { user.Id, user.Email, user.FullName, Role = user.Role.ToString() }
        }));
    }
}

public record LoginRequest(string Email, string Password);
