using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentModes;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.PaymentModes
{
    public interface IPaymentModeService
    {
        Task<List<PaymentModeSetupViewModel>> GetAllAsync();
        Task<PaymentModeSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(PaymentModeSetupViewModel entityVM);
        Task<bool> UpdateAsync(PaymentModeSetupViewModel entityVM);

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsPaymentModeExistAsync(string name);
        Task<bool> IsPaymentModeExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy compatibility methods
        List<SalesDefPaymentMode> GetPaymentModes();
        SalesDefPaymentMode GetPaymentMode(string code);
        bool DeletePaymentMode(string id);
        SalesDefPaymentMode SavePaymentMode(SalesDefPaymentMode entity);
        bool IsPaymentModeExistByCode(string code);
        bool IsPaymentModeExist(string name);
        bool IsPaymentModeExist(string name, string typeCode);
        IEnumerable<CommonSelectModel> PaymentModeSelection();
        IEnumerable<CommonSelectModel> PaymentModeSelectionOnlyCash();
    }
}