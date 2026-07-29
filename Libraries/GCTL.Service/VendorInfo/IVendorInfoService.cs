using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.VendorInfo;
using GCTL.Core.ViewModels.VendorPrefix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.VendorInfo
{
    public interface IVendorInfoService
    {
        Task<List<VendorInfoSetupViewModel>> GetAllAsync();
        Task<VendorInfoSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(VendorInfoSetupViewModel entityVM);
        Task<bool> UpdateAsync(VendorInfoSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionVendorInfoAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
        Task UpdateAsync(VendorPrefixSetupViewModel modelVM);
        Task SaveAsync(VendorPrefixSetupViewModel modelVM);
        Task<CommonReturnViewModel> GetContactPersonsForTableAsync();
    }
}
