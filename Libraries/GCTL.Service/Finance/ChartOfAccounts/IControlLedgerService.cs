using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public interface IControlLedgerService
    {
        Task<PaginationService<AccControlLedger, GetAllControlLedgerVM>.PaginationResult<GetAllControlLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? groupCode = null, bool showArchived = false);

        Task<CommonReturnViewModel> GetByIdAsync(string code);
        Task<CommonReturnViewModel> AddAsync(ControlLedgerAddVM model);
        Task<CommonReturnViewModel> UpdateAsync(ControlLedgerAddVM model);
        Task<CommonReturnViewModel> SoftDeleteAsync(DeleteLedgerRequestVM requestVM);
        Task<CommonReturnViewModel> ToggleArchiveAsync(string code);
        Task<string> GetNextCodeAsync(string groupCode);
        Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? groupCode = null);
    }
}
