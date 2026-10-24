using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public interface ISubSubsidiaryLedgerService
    {
        Task<PaginationService<AccSubsidiaryLedger, GetAllSubSubsidiaryLedgerVM>.PaginationResult<GetAllSubSubsidiaryLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false);

        Task<CommonReturnViewModel> GetByIdAsync(string code);
        Task<CommonReturnViewModel> AddAsync(SubSubsidiaryLedgerAddVM model);
        Task<CommonReturnViewModel> UpdateAsync(SubSubsidiaryLedgerAddVM model);
        Task<CommonReturnViewModel> SoftDeleteAsync(DeleteLedgerRequestVM requestVM);
        Task<CommonReturnViewModel> ToggleArchiveAsync(string code);
        Task<string> GetNextCodeAsync(string subControlCode);
        Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? subControlCode = null);
    }
}
