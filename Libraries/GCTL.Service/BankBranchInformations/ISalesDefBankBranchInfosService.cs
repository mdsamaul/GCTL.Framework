using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SalesDefBankBranchInfos;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.BankBranchInformations
{
    public interface ISalesDefBankBranchInfosService
    {
        Task<List<SalesDefBankBranchInfoSetupViewModel>> GetAllAsync();
        Task<SalesDefBankBranchInfoSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(SalesDefBankBranchInfoSetupViewModel entityVM);
        Task<bool> UpdateAsync(SalesDefBankBranchInfoSetupViewModel entityVM);
        SalesDefBankBranchInfo GetLeaveType(string code);
        bool DeleteLeaveType(string id);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
        Task<string> GenearateNextCode();
        IEnumerable<CommonSelectModel> BankBranchDropSelectionAsync();

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode, string bankId);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
