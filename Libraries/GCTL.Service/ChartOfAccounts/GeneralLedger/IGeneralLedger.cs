using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ChartOfAccounts.GeneralLedger
{
    public interface IGeneralLedger
    {
        Task<List<GeneralLedgerVM>> GetAllAsync();
        Task<GeneralLedgerVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(GeneralLedgerVM model);
        Task<bool> UpdateAsync(GeneralLedgerVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccSubSubsidiaryLedger, GeneralLedgerVM>.PaginationResult<GeneralLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubSusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "");
        Task<string> GetLastGeneralLedgerCodeAsync(string subsidiaryLedgercode);
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(GeneralLedgerVM model);


        #region Sub-Sidiary Dropdown
        Task<List<CommonChoiceVM>> SubSidiaryLedgerDropdown(string subcontrolcode);
        Task<AccSubsidiaryLedger> SubSidiaryLedgerInfo(string subsidiarycode);

        #endregion

        #region Cash Flow Dropdown
        Task<List<CommonChoiceVM>> CashFlowDropdown();

        #endregion
    }
}
