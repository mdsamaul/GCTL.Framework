using GCTL.Core.ViewModels.CurrencyType;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.CurrencyType
{
    public interface ICurrencyType
    {
        Task<List<CurrencyTypeVM>> GetAllAsync();
        Task<CurrencyTypeVM> GetByIdAsync(decimal id);
        Task<bool> SaveAsync(CurrencyTypeVM model);
        Task<bool> UpdateAsync(CurrencyTypeVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(decimal id);
        Task<PaginationService<CaDefCurrency, CurrencyTypeVM>.PaginationResult<CurrencyTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "",
        string sortColumn = "CurrencyId", string sortOrder = "asc");
        Task<string> GetLastCurrencyAsync();
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(CurrencyTypeVM model);

        Task<List<CurrencyTypeVM>> GetAllCurrencyAsync();

        // Setup ViewModel methods for DynamicGrid
        Task<List<CurrencyTypeSetupViewModel>> GetAllSetupAsync();
        Task<CurrencyTypeSetupViewModel> GetSetupByIdAsync(string id);
        Task<bool> SaveSetupAsync(CurrencyTypeSetupViewModel modelVM);
        Task<bool> UpdateSetupAsync(CurrencyTypeSetupViewModel modelVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
        Task<string> GenerateNextCode();
        Task<bool> IsExistByNameAsync(string name, string code);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
