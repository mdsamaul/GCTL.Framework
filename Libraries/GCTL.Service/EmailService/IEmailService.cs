using GCTL.Core.ViewModels.Email;

namespace GCTL.Service.EmailService
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendSingleEmailAsync(string to, string subject, string body, Dictionary<string, string> inlineImages);
        Task<bool> SendSingleEmailAsync(string to, string subject, string body);
        Task SendBatchEmailsAsync(List<EmailDataDto> request, int batchSize = 10, int delayBetweenBatches = 1000);

    }
}
