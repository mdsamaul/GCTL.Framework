using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ImporterInfo;
using GCTL.Core.ViewModels.TransportInformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.ImporterInfo
{
    public interface IImporterInfoService
    {
        Task<List<ImporterInfoSetupViewModel>> GetAllAsync();
        Task<ImporterInfoSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(ImporterInfoSetupViewModel entityVM);
        Task<bool> UpdateAsync(ImporterInfoSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionImporterInfoAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        Task<string> Autoid();

    }
}
