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
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<AuthService> _logger;

    public AuthService(CloudCoreDbContext context, IFluentEmail fluentEmail, ILogger<AuthService> logger)
    {
        _context = context;
        _fluentEmail = fluentEmail;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash) || user.IsEmailVerified == false)
            return null;

        var token = GenerateJwtToken(user);

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
            var emailToken = GenerateEmailVerificationToken(user);
            var verifyUrl = $"https://localhost:3443/verify-email.html?token={emailToken}";

            var contentRoot = AppContext.BaseDirectory;
            var templatePath = Path.Combine(contentRoot, "EmailTemplates", "VerifyEmail.cshtml");
            string template = File.ReadAllText(templatePath);
            string htmlBody = template.Replace("{{VerifyUrl}}", verifyUrl);

            await _fluentEmail
                .To(user.Email)
                .Subject("Welcome to CloudCore - Verify your email")
                .Body(htmlBody, isHtml: true)
                .SendAsync();
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

    public string GenerateJwtToken(User user)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: "CloudCore",
            audience: "CloudCore",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateEmailVerificationToken(User user)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: "CloudCore",
            audience: "CloudCore",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(10),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string?> ConfirmEmailAndGenerateTokenAsync(string token)
    {
        var isValid = await VerifyEmailTokenAsync(token);
        if (!isValid)
            return null;

        var userId = GetUserIdFromToken(token);
        if (userId == null)
            return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        user.IsEmailVerified = true;
        await _context.SaveChangesAsync();

        return GenerateJwtToken(user);
    }

    private int? GetUserIdFromToken(string token)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = "CloudCore",
                ValidAudience = "CloudCore",
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
            }, out _);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> VerifyEmailTokenAsync(string token)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = "CloudCore",
                ValidAudience = "CloudCore",
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return false;

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsEmailVerified = true;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email verification token validation failed.");
            return false;
        }
    }
}
