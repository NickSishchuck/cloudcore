using CloudCore.Domain.Entities;
using System.Security.Claims;

namespace CloudCore.Services.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for authenticated user
        /// </summary>
        /// <param name="user">User for whom to create the token</param>
        /// <returns>JWT token string containing user claims</returns>
        string GenerateJwtToken(User user);
        string GenerateEmailVerificationToken(User user);
        string GenerateEmailChangeToken(User user, string newEmail);

        Task<bool> VerifyEmailTokenAsync(string token);
        ClaimsPrincipal? ValidateToken(string token);
        int? GetUserIdFromToken(string token);
    }
}
