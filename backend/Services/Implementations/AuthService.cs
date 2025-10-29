using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using CloudCore.Common.Models;
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

    public bool VerifyPassword(string password, string storedPassword) //FIXME transfer to validation service
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
}
