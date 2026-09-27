using GCTL.Core.ViewModels.BranchAddress;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ShipmentModeLedgerSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ShipmentModeLedgerSetup
{
    public interface IShipmentModeLedgerSetupService
    {

        Task<List<ShipmentModeLedgerSetupSetupViewModel>> GetAllAsync();
        Task<ShipmentModeLedgerSetupSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(ShipmentModeLedgerSetupSetupViewModel entityVM);
        Task<bool> UpdateAsync(ShipmentModeLedgerSetupSetupViewModel entityVM);

        Task<IEnumerable<CommonSelectModel>> SelectionShipmentModeLedgerAsync();

        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        #region Dropdown
        Task<IEnumerable<CommonSelectModel>> SelectShipmentMode();
        Task<IEnumerable<CommonSelectModel>> SelectRevenueLedger();
        Task<IEnumerable<CommonSelectModel>> SelectExpenseLedger();
        Task<IEnumerable<CommonSelectModel>> SelectAdvanceLedger();
        #endregion

    }
}
