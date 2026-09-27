using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ChartOfAccounts.SubSidiaryLedger
{
    public interface ISubSidiaryLedger
    {
        Task<List<SubsidiaryLedgerVM>> GetAllAsync(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "");
        Task<SubsidiaryLedgerVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(SubsidiaryLedgerVM model);
        Task<bool> UpdateAsync(SubsidiaryLedgerVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccSubsidiaryLedger, SubsidiaryLedgerVM>.PaginationResult<SubsidiaryLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "");
        Task<string> GetLastSubSidiaryLedgerCodeAsync(string subcontrolLedgercode);
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(SubsidiaryLedgerVM model);


        #region Sub-Control Dropdown
        Task<List<CommonChoiceVM>> SubControlLedgerDropdown(string controlcode);
        Task<AccGeneralLedger> SubControlLedgerInfo(string subcontrlcode);
        #endregion
    }
}
