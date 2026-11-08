using System.Security.Cryptography;
using System.Text;
using GCTL.Core.ViewModels.ForgotPasswords;
using GCTL.Data.Models;
using GCTL.Service.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GCTL.Service.PasswordReset
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly GCTL_ERP_DBContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PasswordResetService> _logger;

        private const int OTP_EXPIRATION_MINUTES = 5;
        private const int RATE_LIMIT_ATTEMPTS = 5;
        private const int RATE_LIMIT_WINDOW_MINUTES = 5;
        private const string OTP_PREFIX = "OTP_";
        private const string RATE_LIMIT_PREFIX = "RATE_";

        public PasswordResetService(
            GCTL_ERP_DBContext context,
            IEmailService emailService,
            IMemoryCache cache,
            ILogger<PasswordResetService> logger)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(bool success, string message)> SendOtpAsync(string username, string baseUrl, string? logoPath = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return (false, "Username is required.");
                }

                username = username.Trim();

                if (!CheckRateLimit(username))
                {
                    return (false, "Too many attempts. Please try again after 5 minutes.");
                }

                var user = await _context.CoreUserInfos
                    .FirstOrDefaultAsync(x => x.Username == username);

                if (user == null)
                {
                    IncrementRateLimitAttempt(username);
                    return (false, "User account not found.");
                }

                // Resolve user's official email
                string? targetEmail = null;
                string? companyCode = null;

                if (!string.IsNullOrEmpty(user.EmployeeId))
                {
                    var officialInfo = await _context.HrmEmployeeOfficialInfos
                        .Where(x => x.EmployeeId == user.EmployeeId)
                        .Select(x => new { x.Email, x.CompanyCode })
                        .FirstOrDefaultAsync();

                    targetEmail = officialInfo?.Email;
                    companyCode = officialInfo?.CompanyCode;
                }

                if (string.IsNullOrWhiteSpace(targetEmail))
                {
                    if (!string.IsNullOrWhiteSpace(user.OffEmail))
                    {
                        targetEmail = user.OffEmail;
                    }
                    else if (!string.IsNullOrWhiteSpace(user.PerEmail))
                    {
                        targetEmail = user.PerEmail;
                    }
                }

                if (string.IsNullOrWhiteSpace(targetEmail))
                {
                    IncrementRateLimitAttempt(username);
                    return (false, "No registered email address found for this account. Please contact your system administrator.");
                }

                string companyName = "Formosa ERP";
                if (!string.IsNullOrEmpty(companyCode))
                {
                    var comp = await _context.CoreCompanies
                        .Where(x => x.CompanyCode == companyCode)
                        .Select(x => x.CompanyName)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(comp))
                    {
                        companyName = comp;
                    }
                }

                var otp = GenerateOtp();
                var hashedOtp = HashOtp(otp);
                StoreOtp(username, hashedOtp);

                var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = user.Username;
                }

                var subject = $"{companyName} - Password Reset Verification Code";
                var body = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Password Reset</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f7fb;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">
  <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f4f7fb;"">
    <tr>
      <td style=""padding:40px 20px;"">
        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%""
          style=""max-width:560px;margin:0 auto;background-color:#ffffff;border-radius:12px;border:1px solid #e2e8f0;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.06);"">
          <!-- TOP BAR -->
          <tr>
            <td style=""background:linear-gradient(135deg, #0284c7, #0ea5e9);height:5px;"">&nbsp;</td>
          </tr>
          <!-- HEADER -->
          <tr>
            <td style=""padding:24px 28px 20px;border-bottom:1px solid #e2e8f0;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                <tr>
                  <td style=""vertical-align:middle;"">
                    <img src=""cid:logo"" height=""40"" alt=""{{CompanyName}}"" style=""display:block;border:0;outline:none;"" />
                  </td>
                  <td style=""text-align:right;vertical-align:middle;"">
                    <p style=""margin:0;font-size:12px;font-weight:600;color:#0284c7;"">{{CompanyName}}</p>
                    <p style=""margin:2px 0 0;font-size:11px;color:#94a3b8;"">{{RequestDate}}</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <!-- GREETING -->
          <tr>
            <td style=""padding:28px 28px 0;"">
              <h2 style=""margin:0 0 12px;font-size:18px;font-weight:700;color:#0f172a;"">Password Reset Request</h2>
              <p style=""margin:0;font-size:14px;line-height:24px;color:#475569;"">
                Hello <strong style=""color:#0f172a;"">{{UserFullName}}</strong>,
              </p>
              <p style=""margin:8px 0 0;font-size:14px;line-height:24px;color:#475569;"">
                We received a request to reset your password. Use the verification code below to complete the reset process:
              </p>
            </td>
          </tr>
          <!-- OTP BOX -->
          <tr>
            <td style=""padding:24px 28px;"">
              <div style=""background:#f0f9ff;border:1px solid #bae6fd;border-radius:10px;padding:20px;text-align:center;"">
                <p style=""margin:0 0 8px;font-size:11px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:#0369a1;"">
                  Your One-Time Password (OTP)
                </p>
                <p style=""margin:0;font-size:36px;font-weight:800;letter-spacing:10px;color:#0284c7;font-family:Consolas,Monaco,'Courier New',monospace;"">
                  {{OtpCode}}
                </p>
                <p style=""margin:8px 0 0;font-size:12px;color:#64748b;"">
                  Valid for <strong>5 minutes</strong> only
                </p>
              </div>
            </td>
          </tr>
          <!-- SECURITY NOTICE -->
          <tr>
            <td style=""padding:0 28px 24px;"">
              <div style=""background:#fffbeb;border:1px solid #fef3c7;border-radius:8px;padding:14px 16px;"">
                <p style=""margin:0 0 6px;font-size:13px;font-weight:600;color:#92400e;"">Security Reminder</p>
                <ul style=""margin:0;padding-left:18px;font-size:12px;line-height:20px;color:#b45309;"">
                  <li>Never share this code with anyone. System administrators will never ask for your code.</li>
                  <li>If you did not request a password reset, please ignore this email or notify your IT administrator.</li>
                </ul>
              </div>
            </td>
          </tr>
          <!-- FOOTER -->
          <tr>
            <td style=""background-color:#f8fafc;border-top:1px solid #e2e8f0;padding:14px 28px;text-align:center;"">
              <p style=""margin:0;font-size:11px;color:#94a3b8;"">This is an automated system notification. Please do not reply directly to this email.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

                body = body
                    .Replace("{{OtpCode}}", otp)
                    .Replace("{{UserFullName}}", fullName)
                    .Replace("{{CompanyName}}", companyName)
                    .Replace("{{RequestDate}}", DateTime.Now.ToString("dd MMM yyyy, HH:mm"));

                var inlineImages = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    inlineImages["logo"] = logoPath;
                }

                var emailSent = await _emailService.SendSingleEmailAsync(targetEmail, subject, body, inlineImages);

                if (!emailSent)
                {
                    _logger.LogWarning("Email delivery failed for OTP request to user {Username} at {Email}", username, targetEmail);
                    return (false, "Failed to deliver OTP email. Please ensure the email configuration is active or try again later.");
                }

                IncrementRateLimitAttempt(username);

                // Mask email for user hint (e.g. j***@domain.com)
                string maskedEmail = MaskEmail(targetEmail);
                return (true, $"A 6-digit OTP has been sent to your registered email ({maskedEmail}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating or sending OTP for user {Username}", username);
                return (false, "An unexpected error occurred while sending OTP. Please try again later.");
            }
        }

        public async Task<(bool success, string message)> ResetPasswordAsync(ForgotPasswordViewModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Username))
                {
                    return (false, "Username is required.");
                }

                string username = model.Username.Trim();

                // If user selected Secure Code
                if (string.Equals(model.ResetMethod, "secureCode", StringComparison.OrdinalIgnoreCase))
                {
                    // Backend dummy handling as requested
                    return (false, "Secure Code reset is currently disabled. Please select OTP to reset your password.");
                }

                // OTP Verification Flow
                if (string.IsNullOrWhiteSpace(model.Otp))
                {
                    return (false, "OTP code is required.");
                }

                var storedHashedOtp = GetStoredOtp(username);
                if (string.IsNullOrEmpty(storedHashedOtp))
                {
                    return (false, "The OTP has expired or has not been requested. Please request a new code.");
                }

                if (!VerifyOtp(model.Otp.Trim(), storedHashedOtp))
                {
                    return (false, "Invalid OTP code. Please enter the correct 6-digit code.");
                }

                var user = await _context.CoreUserInfos
                    .FirstOrDefaultAsync(x => x.Username == username);

                if (user == null)
                {
                    return (false, "User account not found.");
                }

                // Hash new password using modern PasswordHasher
                var hasher = new PasswordHasher<CoreUserInfo>();
                user.Password = hasher.HashPassword(user, model.NewPassword);
                user.Lip = model.LIP ?? string.Empty;
                user.Lmac = model.LMAC ?? string.Empty;
                user.Ldate = DateTime.Now;
                user.Luser = user.Username;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Invalidate OTP and clear rate limit
                RemoveOtp(username);
                ClearRateLimit(username);

                return (true, "Password has been reset successfully. You can now log in with your new password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Username}", model?.Username);
                return (false, "An error occurred while resetting your password. Please try again.");
            }
        }

        #region Helpers & OTP Cache
        private static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyOtp(string enteredOtp, string hashedOtp)
        {
            var enteredOtpHash = HashOtp(enteredOtp);
            return enteredOtpHash == hashedOtp;
        }

        private void StoreOtp(string username, string hashedOtp)
        {
            var key = OTP_PREFIX + username.ToLowerInvariant();
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES));
            _cache.Set(key, hashedOtp, options);
        }

        private string? GetStoredOtp(string username)
        {
            var key = OTP_PREFIX + username.ToLowerInvariant();
            _cache.TryGetValue(key, out string? hashedOtp);
            return hashedOtp;
        }

        private void RemoveOtp(string username)
        {
            var key = OTP_PREFIX + username.ToLowerInvariant();
            _cache.Remove(key);
        }

        private bool CheckRateLimit(string username)
        {
            var key = RATE_LIMIT_PREFIX + username.ToLowerInvariant();
            if (_cache.TryGetValue(key, out int attempts))
            {
                return attempts < RATE_LIMIT_ATTEMPTS;
            }
            return true;
        }

        private void IncrementRateLimitAttempt(string username)
        {
            var key = RATE_LIMIT_PREFIX + username.ToLowerInvariant();
            if (_cache.TryGetValue(key, out int attempts))
            {
                attempts++;
            }
            else
            {
                attempts = 1;
            }

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(RATE_LIMIT_WINDOW_MINUTES));
            _cache.Set(key, attempts, options);
        }

        private void ClearRateLimit(string username)
        {
            var key = RATE_LIMIT_PREFIX + username.ToLowerInvariant();
            _cache.Remove(key);
        }

        private static string MaskEmail(string email)
        {
            try
            {
                var parts = email.Split('@');
                if (parts.Length != 2) return email;
                var name = parts[0];
                var domain = parts[1];
                if (name.Length <= 2)
                {
                    return $"{name[0]}*@{domain}";
                }
                return $"{name[0]}{new string('*', name.Length - 2)}{name[^1]}@{domain}";
            }
            catch
            {
                return email;
            }
        }
        #endregion
    }
}

