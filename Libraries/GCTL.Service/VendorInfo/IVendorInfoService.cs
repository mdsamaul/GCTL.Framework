using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.VendorInfo;
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

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
