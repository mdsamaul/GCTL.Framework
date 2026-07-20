using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ExporterInfo;
using GCTL.Core.ViewModels.ImporterInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ExporterInfo
{
    public interface IExporterInfoService
    {
        Task<List<ExporterInfoSetupViewModel>> GetAllAsync();
        Task<ExporterInfoSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(ExporterInfoSetupViewModel entityVM);
        Task<bool> UpdateAsync(ExporterInfoSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionExporterInfoAsync();

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
