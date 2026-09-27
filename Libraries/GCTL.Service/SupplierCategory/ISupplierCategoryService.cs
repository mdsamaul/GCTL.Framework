
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierCategory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.SupplierCategory
{
    public interface ISupplierCategoryService
    {
        Task<List<SupplierCategorySetupViewModel>> GetAllAsync();
        Task<SupplierCategorySetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(SupplierCategorySetupViewModel entityVM);
        Task<bool> UpdateAsync(SupplierCategorySetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionSupplierCategoryAsync();

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
