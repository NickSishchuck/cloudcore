
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user info or Unauthorized</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(ApiResponse.Error("Invalid username or password", "INVALID_CREDENTIALS"));

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
        var result = await _authService.RegisterAsync(request);

        if (result == null)
            return BadRequest(ApiResponse.Error("Username or email already exists", "USER_ALREADY_EXISTS"));

        return Ok(result);
    }
}