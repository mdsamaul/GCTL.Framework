using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.UnitType;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.UnitType
{
    public interface IUnitType
    {
        Task<List<UnitTypeVM>> GetAllAsync();
        Task<UnitTypeVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(UnitTypeVM model);
        Task<bool> UpdateAsync(UnitTypeVM model);
        Task<bool> DeleteAsync(int id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<RmgProdDefUnitType, UnitTypeVM>.PaginationResult<UnitTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "",
        string sortColumn = "UnitTypeID", string sortOrder = "desc");
        Task<string> GetLastUnitAsync();
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsDuplicateAsync(UnitTypeVM model);

        Task<IEnumerable<CommonSelectModel>> SelectionUnitTypeAsync();
    }
}
