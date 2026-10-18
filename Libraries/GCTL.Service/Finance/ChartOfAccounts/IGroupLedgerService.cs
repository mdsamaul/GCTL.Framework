using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public interface IGroupLedgerService
    {
        Task<PaginationService<AccGroupLedger, GetAllGroupLedgerVM>.PaginationResult<GetAllGroupLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", bool showArchived = false);

        Task<CommonReturnViewModel> GetByIdAsync(string code);
        Task<CommonReturnViewModel> AddAsync(GroupLedgerAddVM model);
        Task<CommonReturnViewModel> UpdateAsync(GroupLedgerAddVM model);
        Task<CommonReturnViewModel> SoftDeleteAsync(DeleteLedgerRequestVM requestVM);
        Task<CommonReturnViewModel> ToggleArchiveAsync(string code);
        Task<string> GetNextCodeAsync();
        Task<List<LedgerDropdownItemVM>> GetDropdownAsync();
    }
}
