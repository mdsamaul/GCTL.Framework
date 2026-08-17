using GCTL.Core.ViewModels.ClientInformationEntry;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ClientInformationEntry
{
    public interface IClientInformationEntryService
    {
        Task<List<ClientInformationEntrySetupViewModel>> GetAllAsync();
        Task<ClientInformationEntrySetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(ClientInformationEntrySetupViewModel entityVM);
        Task<bool> UpdateAsync(ClientInformationEntrySetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionClientInformationAsync();

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
