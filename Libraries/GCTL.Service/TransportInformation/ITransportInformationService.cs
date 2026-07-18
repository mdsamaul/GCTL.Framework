using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortName;
using GCTL.Core.ViewModels.TransportInformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.TransportInformation
{
    public interface ITransportInformationService
    {
        Task<List<TransportInformationSetupViewModel>> GetAllAsync();
        Task<TransportInformationSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(TransportInformationSetupViewModel entityVM);
        Task<bool> UpdateAsync(TransportInformationSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionTransportInformationAsync();

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
