using GCTL.Core.ViewModels.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace GCTL.Service.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly SemaphoreSlim _semaphore;
        private readonly IHostEnvironment hostEnvironment;

        public EmailService(IOptions<EmailSettings> emailSettings, IHostEnvironment hostEnvironment)
        {
            _emailSettings = emailSettings.Value;
            _semaphore = new SemaphoreSlim(10);
            this.hostEnvironment = hostEnvironment;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            using (var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
            {
                client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
                client.EnableSsl = _emailSettings.EnableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.MailFrom),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
            }
            return true;
        }

        public async Task<bool> SendSingleEmailAsync(string to, string subject, string body)
        {
            using (var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
            {
                client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
                client.EnableSsl = _emailSettings.EnableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.MailFrom),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                return true;
            }
        }

        public async Task<bool> SendSingleEmailAsync(string to, string subject, string body, Dictionary<string, string> inlineImages)
        {
            await _semaphore.WaitAsync();
            try
            {
                using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
                client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Timeout = 15000;

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.MailFrom),
                    Subject = subject,
                };
                mailMessage.To.Add(to);

                var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

                if(inlineImages != null)
                {
                    foreach(var img in inlineImages)
                    {
                        if (File.Exists(img.Value))
                        {
                            var resource = new LinkedResource(img.Value, "image/png")
                            {
                                ContentId = img.Key,
                                TransferEncoding = TransferEncoding.Base64
                            };
                            htmlView.LinkedResources.Add(resource);
                        }
                    }
                }

                mailMessage.AlternateViews.Add(htmlView);
                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally { _semaphore.Release(); }
        }

        public async Task<bool> SendSingleEmailAsync(string to, string subject, string body, string attachmentPath = null, string attachmentname = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
                client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Timeout = 15000;

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.MailFrom),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    if (!string.IsNullOrEmpty(attachmentname))
                        attachment.Name = attachmentname;
                    mailMessage.Attachments.Add(attachment);
                }

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SendBatchEmailsAsync(List<EmailDataDto> request, int batchSize = 10, int delayBetweenBatches = 1000)
        {
            var results = new List<EmailResult>();
            var batches = request
                .Select((email, index) => new { email, index })
                .GroupBy(x => x.index / batchSize)
                .Select(group => group.Select(x => x.email).ToList())
                .ToList();
            var startTime = DateTime.Now;

            foreach (var (batch, batchIndex) in batches.Select((b, i) => (b, i)))
            {
                var batchTasks = batch.Select(async request =>
                {
                    try
                    {

                        await SendSingleEmailAsync(request.To, request.Subject, request.Body, request.AttachmentPath, request.AttachmentName);
                        return new EmailResult { Email = request.To, isSuccess = true };
                    }
                    catch (Exception ex)
                    {
                        return new EmailResult { Email = request.To, isSuccess = false, Error = ex.Message };
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);
                results.AddRange(batchResults);

                var successCount = batchResults.Count(r => r.isSuccess);
                var failedCount = batchResults.Count(r => !r.isSuccess);



                if (batchIndex < batches.Count - 1 && delayBetweenBatches > 0)
                {
                    await Task.Delay(delayBetweenBatches);
                }
            }

            var totalSuccess = results.Count(r => r.isSuccess);
            var totalFailed = results.Count(r => !r.isSuccess);
            var elapsed = DateTime.Now - startTime;

            Console.WriteLine($"Email batch completed in {elapsed.TotalMinutes:F1} minutes: {totalSuccess} sent, {totalFailed} failed");
        }
        public string GenerateLeaveRequestEmail(string employeeName, string leaveType, string startDate, string endDate, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f8f9fa;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
    <tr>
      <td align=""center"" style=""padding:20px;"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f9f9f9;padding:20px;"">
          <tr>
            <td>
              <h1 style=""color:#5a5a5a;font-family:Arial,sans-serif;margin:0 0 16px 0;"">Leave Request Submitted</h1>
              <p style=""font-family:Arial,sans-serif;margin:0 0 8px 0;"">Dear {employeeName},</p>
              <p style=""font-family:Arial,sans-serif;margin:0 0 16px 0;"">We have received your leave request:</p>
              <table width=""100%"" cellpadding=""8"" cellspacing=""0"" border=""1"" style=""border-collapse:collapse;border-color:#ddd;"">
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Leave Type</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{leaveType}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Start Date</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{startDate}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">End Date</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{endDate}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Reason</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{reason}</td>
                </tr>
              </table>
              <p style=""font-family:Arial,sans-serif;margin:16px 0 0 0;"">Thank you,<br/>The HR Team</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        // Generate Leave Approval Email
        public string GenerateLeaveApprovalEmail(string employeeName, string leaveType, string startDate, string endDate)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f8f9fa;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
    <tr>
      <td align=""center"" style=""padding:20px;"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f9f9f9;padding:20px;"">
          <tr>
            <td>
              <h1 style=""color:#28a745;font-family:Arial,sans-serif;margin:0 0 16px 0;"">Leave Request Approved</h1>
              <p style=""font-family:Arial,sans-serif;margin:0 0 8px 0;"">Dear {employeeName},</p>
              <p style=""font-family:Arial,sans-serif;margin:0 0 16px 0;"">Your leave request has been approved:</p>
              <table width=""100%"" cellpadding=""8"" cellspacing=""0"" border=""1"" style=""border-collapse:collapse;border-color:#ddd;"">
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Leave Type</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{leaveType}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Start Date</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{startDate}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">End Date</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{endDate}</td>
                </tr>
              </table>
              <p style=""font-family:Arial,sans-serif;margin:16px 0 0 0;"">Enjoy your leave!<br/>The HR Team</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        // Generate Leave Rejection Email
        public string GenerateLeaveRejectionEmail(string employeeName, string leaveType, string rejectionReason)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f8f9fa;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
    <tr>
      <td align=""center"" style=""padding:20px;"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f9f9f9;padding:20px;"">
          <tr>
            <td>
              <h1 style=""color:#dc3545;font-family:Arial,sans-serif;margin:0 0 16px 0;"">Leave Request Rejected</h1>
              <p style=""font-family:Arial,sans-serif;margin:0 0 8px 0;"">Dear {employeeName},</p>
              <p style=""font-family:Arial,sans-serif;margin:0 0 16px 0;"">We regret to inform you that your leave request has been rejected:</p>
              <table width=""100%"" cellpadding=""8"" cellspacing=""0"" border=""1"" style=""border-collapse:collapse;border-color:#ddd;"">
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Leave Type</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{leaveType}</td>
                </tr>
                <tr>
                  <th align=""left"" style=""font-family:Arial,sans-serif;background-color:#f2f2f2;border:1px solid #ddd;padding:8px;"">Reason</th>
                  <td style=""font-family:Arial,sans-serif;border:1px solid #ddd;padding:8px;"">{rejectionReason}</td>
                </tr>
              </table>
              <p style=""font-family:Arial,sans-serif;margin:16px 0 0 0;"">Please contact your manager for more details.<br/>The HR Team</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

    }
}
