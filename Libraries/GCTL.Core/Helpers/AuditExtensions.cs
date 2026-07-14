using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;

namespace GCTL.Core.Helpers
{
    public static class AuditExtensions
    {
        // Existing helper for older codepaths that pass a UserInfoViewModel
        public static BaseViewModel ToAudit(this BaseViewModel model, UserInfoViewModel info, bool isUpdate = false)
        {
            model.Lip = info?.IPAddress ?? string.Empty;
            model.Lmac = info?.MacAddress ?? string.Empty;
            model.Luser = info?.Username ?? string.Empty;
            model.UserInfoEmployeeId = info?.EmployeeId ?? string.Empty;
            if (isUpdate)
                model.ModifyDate = DateTime.Now;
            else
                model.Ldate = DateTime.Now;

            return model;
        }
    }
}
