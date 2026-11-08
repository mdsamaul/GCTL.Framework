using GCTL.Core.ViewModels.ForgotPasswords;

namespace GCTL.Service.PasswordReset
{
    public interface IPasswordResetService
    {
        Task<(bool success, string message)> SendOtpAsync(string username, string baseUrl, string? logoPath = null);
        Task<(bool success, string message)> ResetPasswordAsync(ForgotPasswordViewModel model);
    }
}

