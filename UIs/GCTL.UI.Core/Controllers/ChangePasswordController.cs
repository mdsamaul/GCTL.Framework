using System.Net.NetworkInformation;
using GCTL.Core.ViewModels.ForgotPasswords;
using GCTL.Service.PasswordReset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    [AllowAnonymous]
    public class ChangePasswordController : Controller
    {
        private readonly IPasswordResetService _passwordResetService;
        private readonly IWebHostEnvironment _env;

        public ChangePasswordController(
            IPasswordResetService passwordResetService,
            IWebHostEnvironment env)
        {
            _passwordResetService = passwordResetService;
            _env = env;
        }

        [HttpGet]
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword(string? username)
        {
            var model = new ForgotPasswordViewModel
            {
                Username = username ?? string.Empty,
                ResetMethod = "otp"
            };

            return View(model);
        }

        [HttpPost]
        [Route("SendOtpForForgotPassword")]
        public async Task<IActionResult> SendOtpForForgotPassword(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new { success = false, message = "Username is required." });
            }

            string logoPath = Path.Combine(_env.WebRootPath, "images", "fermosa-wX200.png");
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(_env.WebRootPath, "images", "gctl.png");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var (success, message) = await _passwordResetService.SendOtpAsync(username.Trim(), baseUrl, logoPath);

            return Json(new { success, message });
        }

        [HttpPost]
        [Route("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.LIP = GetClientIP();
            model.LMAC = GetClientMAC();

            var (success, message) = await _passwordResetService.ResetPasswordAsync(model);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewData["ErrorMessage"] = message;
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Login", "Account");
        }

        private string GetClientIP()
        {
            try
            {
                string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    return ip;
                }

                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var net in networkInterfaces)
                {
                    var ipv4 = net.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ipv4 != null)
                    {
                        return ipv4.Address.ToString();
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return "127.0.0.1";
        }

        private string GetClientMAC()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var net in networkInterfaces)
                {
                    var mac = net.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        return mac;
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return string.Empty;
        }
    }
}

