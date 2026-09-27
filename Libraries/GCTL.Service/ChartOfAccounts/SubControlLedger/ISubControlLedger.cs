using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ChartOfAccounts.SubControlLedger
{
    public interface ISubControlLedger
    {
        Task<List<SubControlLedgerVM>> GetAllAsync(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "");
        Task<SubControlLedgerVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(SubControlLedgerVM model);
        Task<bool> UpdateAsync(SubControlLedgerVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccGeneralLedger, SubControlLedgerVM>.PaginationResult<SubControlLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "GeneralLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "");
        Task<string> GetLastSubControlLedgerCodeAsync(string controlLedgercode);
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(SubControlLedgerVM model);

        #region Control Ledger Dropdown
        Task<List<CommonChoiceVM>> ControlLedgerDropdown(string groupcode);
        Task<AccSubControlLedger> GetAllControlInfo(string contrlcode);

        #endregion
    }
}
