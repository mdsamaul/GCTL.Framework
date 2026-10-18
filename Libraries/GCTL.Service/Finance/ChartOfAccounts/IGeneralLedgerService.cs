using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public interface IGeneralLedgerService
    {
        Task<PaginationService<AccGeneralLedger, GetAllGeneralLedgerVM>.PaginationResult<GetAllGeneralLedgerVM>> GetAllAsync(
            int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subSubsidiaryCode = null, string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false);

        Task<CommonReturnViewModel> GetByIdAsync(string code);
        Task<CommonReturnViewModel> AddAsync(GeneralLedgerAddVM model);
        Task<CommonReturnViewModel> UpdateAsync(GeneralLedgerAddVM model);
        Task<CommonReturnViewModel> SoftDeleteAsync(DeleteLedgerRequestVM requestVM);
        Task<CommonReturnViewModel> ToggleArchiveAsync(string code);
        Task<string> GetNextCodeAsync(string subSubsidiaryCode);
        Task<List<LedgerDropdownItemVM>> GetDropdownAsync(string? subSubsidiaryCode = null);
    }
}
