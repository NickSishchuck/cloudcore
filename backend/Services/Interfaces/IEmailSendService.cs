namespace CloudCore.Services.Interfaces
{
    public interface IEmailSendService
    {
        Task SendEmailVerificationAsync(string toEmail, string verifyUrl, string subject);
    }
}
