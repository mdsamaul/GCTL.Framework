using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportModeType;
using GCTL.Core.ViewModels.TransportType;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.ModeType
{
    public interface IModeTypeService
    {
        Task<List<TransportModeTypeSetupViewModel>> GetAllAsync();
        Task<TransportModeTypeSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(TransportModeTypeSetupViewModel entityVM);
        Task<bool> UpdateAsync(TransportModeTypeSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionTransportModeTypeAsync();
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
