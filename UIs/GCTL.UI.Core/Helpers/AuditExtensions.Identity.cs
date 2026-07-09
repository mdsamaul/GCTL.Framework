using GCTL.Core.ViewModels;
using GCTL.Data.Identity;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace GCTL.UI.Core.Helpers
{
    public static class AuditExtensionsIdentity
    {
        public static BaseViewModel ToAudit(
            this BaseViewModel model,
            ApplicationUser user,
            HttpContext context,
            bool isUpdate = false)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";

            model.Lip = ip;

            model.Lmac = NetworkHelper.GetMacAddress(ip);

            model.Luser = user.UserName ?? "";

            model.UserInfoEmployeeId = user.EmployeeId ?? "";

            if (isUpdate)
                model.ModifyDate = DateTime.Now;
            else
                model.Ldate = DateTime.Now;

            return model;
        }

        // Overload that allows calling `model.ToAudit(LoginInfo, model.Id > 0)` from controllers.
        // It resolves IHttpContextAccessor from the ServiceLocator to obtain the current HttpContext.
        public static BaseViewModel ToAudit(this BaseViewModel model, ApplicationUser user, bool isUpdate = false)
        {
            string ip = string.Empty;
            string mac = string.Empty;

            try
            {
                var accessor = ServiceLocator.ServiceProvider?.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
                var ctx = accessor?.HttpContext;
                if (ctx != null)
                {
                    // Prefer explicit headers set by proxies or client agent (MAC header first for completeness)
                    mac = ctx.Request.Headers["X-MAC-Address"].FirstOrDefault() ?? string.Empty;

                    // Try common forwarded headers for client IP
                    ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrEmpty(ip))
                        ip = ctx.Request.Headers["X-Real-IP"].FirstOrDefault() ?? string.Empty;

                    // If header contains multiple addresses, take first
                    if (!string.IsNullOrEmpty(ip) && ip.Contains(","))
                        ip = ip.Split(',')[0].Trim();

                    // Fallback to connection remote IP
                    if (string.IsNullOrEmpty(ip))
                        ip = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

                    // Normalize IPv6 loopback and IPv4-mapped addresses
                    if (!string.IsNullOrEmpty(ip))
                    {
                        if (ip == "::1")
                        {
                            ip = "127.0.0.1";
                        }
                        else if (IPAddress.TryParse(ip, out var parsed))
                        {
                            if (parsed.IsIPv4MappedToIPv6)
                                ip = parsed.MapToIPv4().ToString();
                        }
                    }
                }
            }
            catch
            {
                ip = string.Empty;
                mac = string.Empty;
            }

            model.Lip = ip;
            if (string.IsNullOrEmpty(mac) && !string.IsNullOrEmpty(ip))
            {
                // Attempt ARP lookup only when we have an IP and no header-provided MAC
                mac = NetworkHelper.GetMacAddress(ip);
            }
            model.Lmac = mac ?? string.Empty;
            model.Luser = user?.UserName ?? user?.FullName ?? string.Empty;
            model.UserInfoEmployeeId = user?.EmployeeId ?? string.Empty;

            if (isUpdate)
                model.ModifyDate = DateTime.Now;
            else
                model.Ldate = DateTime.Now;

            return model;
        }
    }
}