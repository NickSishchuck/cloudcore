
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
}