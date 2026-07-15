using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OpeningBalance;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.OpeningBalance
{
    public interface IOpeningBalance
    {
        Task<List<OpeningBalanceVM>> GetAllAsync();
        Task<OpeningBalanceVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(OpeningBalanceVM model);
        Task<bool> UpdateAsync(OpeningBalanceVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        //Task<PaginationService<Acc_Company_Opening_Balance, OpeningBalanceVM>.PaginationResult<OpeningBalanceVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "ComOpeningBalanceCode", string sortOrder = "desc");

        Task<PaginationService<OpeningBalanceVM, OpeningBalanceVM>.PaginationResult<OpeningBalanceVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "ComOpeningBalanceCode", string sortOrder = "desc");
        Task<string> GetLastOpeiningBalanceCodeAsync();
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        //Task<bool> IsDuplicateAsync(OpeningBalanceVM model);

        #region Company & Branch & General Ledger Dropdown

        Task<List<CommonChoiceVM>> GetCompanyDropdownInfo();
        Task<List<CommonChoiceVM>> GetBranchDropdownInfo(string companycode);

        Task<List<CommonChoiceVM>> GetGenetalLedegerDropdownInfo();

        #endregion
    }
}
