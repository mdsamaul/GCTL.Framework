using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.BranchAddress;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.BranchAddress
{
    public interface IBranchAddressService 
    {
        Task<List<BranchAddressSetupViewModel>> GetAllAsync();
        Task<BranchAddressSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(BranchAddressSetupViewModel entityVM);
        Task<bool> UpdateAsync(BranchAddressSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionBranchAddressAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
        Task<CommonReturnViewModel> GetContactPersonsForTableAsync();
    }
}
