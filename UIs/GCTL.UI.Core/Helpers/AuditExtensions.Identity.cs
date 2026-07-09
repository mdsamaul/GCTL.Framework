using GCTL.Core.ViewModels;
using GCTL.Data.Identity;
using Microsoft.AspNetCore.Http;

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
    }
}