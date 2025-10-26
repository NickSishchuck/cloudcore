using CloudCore.Common.Models;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Services.Interfaces;

/// <summary>
/// Service for handling user authentication and authorization operations
/// </summary>
public interface IAuthService
{
    #region Login/Register

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="request">Login request containing username and password credentials</param>
    /// <returns>Authentication response with JWT token and user information, or null if authentication fails</returns>
    Task<AuthResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user in the system
    /// </summary>
    /// <param name="request">Registration request containing new user data</param>
    /// <returns>Authentication response with JWT token and created user information, or null if registration fails</returns>
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);

    #endregion

    #region Password/JWT

    /// <summary>
    /// Creates a secure password hash for database storage
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>Hashed password suitable for secure storage</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies if the provided password matches the stored hash
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hash">Stored password hash for comparison</param>
    /// <returns>True if password is correct, false otherwise</returns>
    bool VerifyPassword(string password, string hash);

    Task<string?> ConfirmEmailAndGenerateTokenAsync(string token);


    Task<bool> ChangeUsernameAsync(int userId, string newUsername);

    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

    Task<bool> SendEmailVerificationAsync(int userId, string newEmail);

    Task<bool> ConfirmEmailChangeAsync(string token);

    Task<bool> UpgradePlanAsync(int userId, SubscriptionPlan subscriptionPlan);
    #endregion

}