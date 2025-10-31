using CloudCore.Common.Models;

namespace CloudCore.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> ChangeUsernameAsync(int userId, string newUsername);

        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        Task<bool> SendEmailVerificationAsync(int userId, string newEmail);

        Task<bool> ConfirmEmailChangeAsync(string token);

        Task<bool> UpgradePlanAsync(int userId, SubscriptionPlan subscriptionPlan);
    }
}
