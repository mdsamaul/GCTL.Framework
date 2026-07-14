using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ChartOfAccounts.ControlLedger
{
    public interface IContrlLedger
    {
        Task<List<ControlLedgerVM>> GetAllAsync();
        Task<ControlLedgerVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(ControlLedgerVM model);
        Task<bool> UpdateAsync(ControlLedgerVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccSubControlLedger, ControlLedgerVM>.PaginationResult<ControlLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubControlLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "");
        Task<string> GetLastControlLedgerCodeAsync(string groupledgercode);
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(ControlLedgerVM model);

        //Group Ledger Dropdown
        Task<List<CommonChoiceVM>> GroupLedgerDropdown();
        Task<AccControlLedger> GetAllGroupLedger(string groupcode);
    }
}
