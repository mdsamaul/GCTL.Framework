using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentType;
using GCTL.Core.ViewModels.TransportType;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.TransportType
{
    public interface ITransportTypeService 
    {
        Task<List<TransportTypeSetupViewModel>> GetAllAsync();
        Task<TransportTypeSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(TransportTypeSetupViewModel entityVM);
        Task<bool> UpdateAsync(TransportTypeSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionTransportTypeAsync();

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
