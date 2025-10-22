using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Data.Context;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CloudCore.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly CloudCoreDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly ITokenService _tokenService;
    private readonly IEmailSendService _emailSendService;


    public AuthService(CloudCoreDbContext context, IEmailSendService emailSendService, ILogger<AuthService> logger, ITokenService tokenService)
    {
        _context = context;
        _emailSendService = emailSendService;
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash) || user.IsEmailVerified == false)
            return null;

        var token = _tokenService.GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            return null;

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            IsEmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        try
        {
            var emailToken = _tokenService.GenerateEmailVerificationToken(user);
            var verifyUrl = $"https://localhost:3443/verify-email.html?token={emailToken}";

            await _emailSendService.SendEmailVerificationAsync(
                user.Email,
                verifyUrl,
                "Welcome to CloudCore - Verify your email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email");
        }

        return new AuthResponse
        {
            Token = null,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }

    public string HashPassword(string password)
    {
        //return BCrypt.Net.BCrypt.HashPassword(password);
        return password;
    }

    public bool VerifyPassword(string password, string storedPassword)
    {
        //return BCrypt.Net.BCrypt.Verify(password, storedPassword);
        return password == storedPassword;
    }





    public async Task<string?> ConfirmEmailAndGenerateTokenAsync(string token)
    {
        var isValid = await _tokenService.VerifyEmailTokenAsync(token);
        if (!isValid)
            return null;

        var userId = _tokenService.GetUserIdFromToken(token);
        if (userId == null)
            return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        user.IsEmailVerified = true;
        await _context.SaveChangesAsync();

        return _tokenService.GenerateJwtToken(user);
    }


    public async Task<bool> ChangeUsernameAsync(int userId, string newUsername)
    {
        if (await _context.Users.AnyAsync(u => u.Username == newUsername))
            return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Username = newUsername;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!VerifyPassword(oldPassword, user.PasswordHash)) //FIXME use passwordhash
            return false;
        _logger.LogInformation("Password verified successfully.");

        user.PasswordHash = newPassword; //FIXME use passwordhash
        await _context.SaveChangesAsync();
        return true;
    }


    public async Task<bool> SendEmailVerificationAsync(int userId, string newEmail)
    {
        if (await _context.Users.AnyAsync(u => u.Email == newEmail))
            return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        try
        {
            var emailToken = _tokenService.GenerateEmailChangeToken(user, newEmail);

            var verifyUrl = $"https://localhost:3443/verify-email.html?token={emailToken}&type=change";

            await _emailSendService.SendEmailVerificationAsync(
                user.Email,
                verifyUrl,
                "Confirm your new email address");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification.");
            return false;
        }
    }

    public async Task<bool> ConfirmEmailChangeAsync(string token)
    {
        _logger.LogInformation("=== ConfirmEmailChangeAsync called ===");

        var principal = _tokenService.ValidateToken(token);
        if (principal == null)
        {
            _logger.LogError("Token validation failed");
            return false;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        var newEmailClaim = principal.FindFirst("new_email");

        _logger.LogInformation($"UserId claim: {userIdClaim?.Value}");
        _logger.LogInformation($"New email claim: {newEmailClaim?.Value}");

        if (userIdClaim == null || newEmailClaim == null)
        {
            _logger.LogWarning("Required claims missing in email change token.");
            return false;
        }

        if (!int.TryParse(userIdClaim.Value, out int userId))
        {
            _logger.LogWarning("Failed to parse userId");
            return false;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning($"User with ID {userId} not found during email confirmation.");
            return false;
        }

        _logger.LogInformation($"Found user: {user.Username}, current email: {user.Email}");
        _logger.LogInformation($"Changing email to: {newEmailClaim.Value}");

        user.Email = newEmailClaim.Value;
        user.IsEmailVerified = true;

        var changes = await _context.SaveChangesAsync();
        _logger.LogInformation($"SaveChanges returned: {changes} affected rows");

        return true;
    }




}
