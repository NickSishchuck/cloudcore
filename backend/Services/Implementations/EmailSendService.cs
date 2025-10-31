using CloudCore.Services.Interfaces;
using FluentEmail.Core;

namespace CloudCore.Services.Implementations
{
    public class EmailSendService : IEmailSendService
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly ILogger<EmailSendService> _logger;

        public EmailSendService(IFluentEmail fluentEmail, ILogger<EmailSendService> logger)
        {
            _fluentEmail = fluentEmail;
            _logger = logger;
        }

        public async Task SendEmailVerificationAsync(string toEmail, string verifyUrl, string subject)
        {
            try
            {
                var contentRoot = AppContext.BaseDirectory;
                var templatePath = Path.Combine(contentRoot, "EmailTemplates", "VerifyEmail.cshtml");
                string template = File.ReadAllText(templatePath);
                string htmlBody = template.Replace("{{VerifyUrl}}", verifyUrl);

                await _fluentEmail
                    .To(toEmail)
                    .Subject(subject)
                    .Body(htmlBody, isHtml: true)
                    .SendAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw;
            }
        }
    }
}
