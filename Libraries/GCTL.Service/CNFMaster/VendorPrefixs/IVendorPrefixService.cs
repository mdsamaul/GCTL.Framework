using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CNFMaster.VendorPrefixs;
using GCTL.Core.ViewModels.DeleteHistories;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.CNFMaster.VendorPrefixs
{
    public interface IVendorPrefixService
    {
        Task<(List<VendorPrefixSetupVM> Data, int TotalRecords, int filterRecords)> GetPaginatedDataAsync(string searchValue, int page, int pageSize, string sortColumn, string sortDirection);
        Task<VendorPrefixSetupVM> GetByIdAsync(decimal id);
        Task<CommonReturnViewModel> HasDuplicate(string name, string id = null);
        Task<CommonReturnViewModel> SaveAsync(VendorPrefixSetupVM model);
        Task<CommonReturnViewModel> EditAsync(VendorPrefixSetupVM model);
        Task<CommonReturnViewModel> BulkDeleteAsync(List<decimal> ids, DeleteHistoryViewModel model);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
