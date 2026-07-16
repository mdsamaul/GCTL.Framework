using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortType;
using GCTL.Core.ViewModels.SupplierCategory;
using GCTL.Core.ViewModels.SupplierType;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.PortType
{
    public interface IPortTypeService
    {
        Task<List<PortTypeSetupViewModel>> GetAllAsync();
        Task<PortTypeSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(PortTypeSetupViewModel entityVM);
        Task<bool> UpdateAsync(PortTypeSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionPortTypeAsync();

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
