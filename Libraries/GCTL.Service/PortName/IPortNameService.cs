using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentTerms;
using GCTL.Core.ViewModels.PortName;
using GCTL.Core.ViewModels.PortType;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.PortName
{
    public interface IPortNameService
    {
        Task<List<PortNameSetupViewModel>> GetAllAsync();
        Task<PortNameSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(PortNameSetupViewModel entityVM);
        Task<bool> UpdateAsync(PortNameSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionPortNameAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<bool> IsExistAsync(string name, string typeCode, string portId);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

    }
}
