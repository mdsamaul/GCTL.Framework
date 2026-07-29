using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportType;
using GCTL.Core.ViewModels.VendorInfo;
using GCTL.Core.ViewModels.VendorPrefix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.VendorPrefix
{
    public interface IVendorPrefixService
    {
        Task<List<VendorPrefixSetupViewModel>> GetAllAsync();
        Task<VendorPrefixSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(VendorPrefixSetupViewModel entityVM);
        Task<bool> UpdateAsync(VendorPrefixSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionVendorPrefixAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
