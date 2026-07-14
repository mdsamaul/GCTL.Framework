using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ChartOfAccounts.GroupLedger
{
    public interface IGroupLedger
    {
        Task<List<GroupLedgerVM>> GetAllAsync();
        Task<GroupLedgerVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(GroupLedgerVM model);
        Task<bool> UpdateAsync(GroupLedgerVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccControlLedger, GroupLedgerVM>.PaginationResult<GroupLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "",
        string sortColumn = "ControlLedgerCodeNo", string sortOrder = "desc");
        Task<string> GetLastGroupLedgerCodeAsync();
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(GroupLedgerVM model);
    }
}
