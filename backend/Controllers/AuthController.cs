
using System.Security.Claims;
using CloudCore.Common.Errors;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CloudCore.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user info or Unauthorized</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation($"Login attempt for user: {request.Username}.");
        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            _logger.LogWarning($"Failed login attempt for user: {request.Username}.");
            return Unauthorized(ApiResponse.Error("Invalid username or password", "INVALID_CREDENTIALS"));
        }
        _logger.LogInformation($"User {request.Username} (ID: {result.UserId}) logged in successfully.");
        return Ok(result);
    }

    /// <summary>
    /// User registration endpoint
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>JWT token and user info or BadRequest</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation($"Register attempt for user: {request.Username}, (Email: {request.Email}).");
        var result = await _authService.RegisterAsync(request);

        if (result == null)
        {
            _logger.LogWarning($"Failed register attempt for user: {request.Username}, (Email: {request.Email})");
            return BadRequest(ApiResponse.Error("Username or email already exists", "USER_ALREADY_EXISTS"));
        }
        _logger.LogInformation($"User {request.Username} (ID: {result.UserId}) registered successfully.");
        return Ok(result);
    }

    /// <summary>
    /// Email verification endpoint
    /// </summary>
    /// <param name="token">JWT token from email link</param>
    /// <returns>Result of verification</returns>
    [HttpPost("verify-email")]
    public async Task<ActionResult> VerifyEmail([FromBody] TokenRequest token)
    {
        _logger.LogInformation("Email verification attempt.");
        _logger.LogInformation($"Verification token: {token.Token}");
        var jwtToken = await _authService.ConfirmEmailAndGenerateTokenAsync(token.Token);
        if (jwtToken == null)
        {
            _logger.LogWarning("Email verification failed or token invalid.");
            return BadRequest(ApiResponse.Error("Invalid or expired token", "INVALID_TOKEN"));
        }

        return Ok(new AuthResponse
        {
            Token = jwtToken
        });
    }

    [Authorize]
    [HttpPost("{userId}/change-username")]
    public async Task<ActionResult> ChangeUsername(int userId, [FromBody] ChangeUsernameRequest request)
    {
        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
        {
            return Forbid();
        }
        var success = await _authService.ChangeUsernameAsync(userId, request.NewUsername);
        if (!success)
            return BadRequest(ApiResponse.Error("Username already taken", ErrorCodes.USERNAME_EXISTS));

        return Ok(ApiResponse.Ok("Username updated successfully"));
    }

    [Authorize]
    [HttpPost("{userId}/change-password")]
    public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
    {
        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
        {
            return Forbid();
        }
        var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success)
            return BadRequest(ApiResponse.Error("Invalid current password", "INVALID_PASSWORD"));
        return Ok(ApiResponse.Ok("Password changed successfully"));
    }

    [Authorize]
    [HttpPost("{userId}/request-email-change")]
    public async Task<ActionResult> RequestEmailChange(int userId, [FromBody] ChangeEmailRequest request)
    {
        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
            return Forbid();

        var success = await _authService.SendEmailVerificationAsync(userId, request.NewEmail);
        if (!success)
            return BadRequest(ApiResponse.Error("Email already taken or invalid", ErrorCodes.EMAIL_EXISTS));

        return Ok(ApiResponse.Ok("Verification email sent. Please confirm your new email address."));
    }

    [HttpPost("confirm-email-change")]
    public async Task<ActionResult> ConfirmEmailChange([FromBody] TokenRequest token)
    {
        var success = await _authService.ConfirmEmailChangeAsync(token.Token);
        if (!success)
            return BadRequest(ApiResponse.Error("Invalid or expired token", "INVALID_TOKEN"));

        return Ok(ApiResponse.Ok("Email successfully changed and verified."));
    }

}
