using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.UnitType;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.UnitType
{
    public interface IUnitType
    {
        Task<List<UnitTypeSetupViewModel>> GetAllAsync();
        Task<UnitTypeSetupViewModel> GetByIdAsync(int id);
        Task<UnitTypeSetupViewModel> GetByIdAsync(string code);
        Task<bool> SaveAsync(UnitTypeSetupViewModel model);
        Task<bool> UpdateAsync(UnitTypeSetupViewModel model);
        Task<bool> DeleteAsync(int id);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<RmgProdDefUnitType, UnitTypeVM>.PaginationResult<UnitTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "UnitTypID", string sortOrder = "desc");
        Task<string> GetLastUnitAsync();
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsDuplicateAsync(UnitTypeSetupViewModel model);
        Task<bool> IsDuplicateAsync(UnitTypeVM model);
        Task<IEnumerable<CommonSelectModel>> SelectionUnitTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
