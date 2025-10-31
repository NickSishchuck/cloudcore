using CloudCore.Common.Errors;
using CloudCore.Common.Models;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudCore.Services.Interfaces;

namespace CloudCore.Controllers;

[ApiController]
[Route("user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("{userId}/change-username")]
    public async Task<ActionResult> ChangeUsername(int userId, [FromBody] ChangeUsernameRequest request)
    {
        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
        {
            return Forbid();
        }
        var success = await _userService.ChangeUsernameAsync(userId, request.NewUsername);
        if (!success)
            return BadRequest(ApiResponse.Error("Username already taken", ErrorCodes.USERNAME_EXISTS));

        return Ok(ApiResponse.Ok("Username updated successfully"));
    }

    [HttpPost("{userId}/change-password")]
    public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
    {
        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
        {
            return Forbid();
        }
        var success = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!success)
            return BadRequest(ApiResponse.Error("Invalid current password", "INVALID_PASSWORD"));
        return Ok(ApiResponse.Ok("Password changed successfully"));
    }

    [HttpPost("{userId}/request-email-change")]
    public async Task<ActionResult> RequestEmailChange(int userId, [FromBody] ChangeEmailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
        {
            return Forbid();
        }

        _logger.LogInformation("RequestEmailChange called for user {UserId} with email {Email}", userId, request.NewEmail);

        var success = await _userService.SendEmailVerificationAsync(userId, request.NewEmail);
        if (!success)
        {
            _logger.LogWarning("Email change failed for user {UserId} with email {Email}", userId, request.NewEmail);
            return BadRequest(ApiResponse.Error("Email already taken or invalid", ErrorCodes.EMAIL_EXISTS));
        }

        return Ok(ApiResponse.Ok("Verification email sent. Please confirm your new email address."));
    }


    [AllowAnonymous]
    [HttpPost("confirm-email-change")]
    public async Task<ActionResult> ConfirmEmailChange([FromBody] TokenRequest token)
    {
        var success = await _userService.ConfirmEmailChangeAsync(token.Token);
        if (!success)
            return BadRequest(ApiResponse.Error("Invalid or expired token", "INVALID_TOKEN"));

        return Ok(ApiResponse.Ok("Email successfully changed and verified."));
    }

    [Authorize]
    [HttpPost("{userId}/upgrade-plan")]
    public async Task<ActionResult> UpgradePlan([FromRoute] int userId, [FromBody] UpgradePlanRequest upgradePlanRequest)
    {
        if (upgradePlanRequest.NewPlan == null || !Enum.IsDefined(typeof(SubscriptionPlan), upgradePlanRequest.NewPlan))
        {
            _logger.LogWarning("Requested upgrade plan value: {upgradePlanRequest}", upgradePlanRequest);
            return BadRequest(ApiResponse.Error("Invalid subscription plan value"));
        }

        var tokenUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (tokenUserId != userId)
            return Forbid();

        var success = await _userService.UpgradePlanAsync(userId, upgradePlanRequest.NewPlan.Value);
        if (!success)
            return BadRequest(ApiResponse.Error("Error upgrading plan"));
        return Ok(ApiResponse.Ok("Plan upgraded successfully"));
    }
}
