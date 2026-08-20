using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ShipmentMode
{
    public interface IShipmentMode
    {
        Task<List<ShipmentModeVM>> GetAllAsync();
        Task<ShipmentModeVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(ShipmentModeVM model);
        Task<bool> UpdateAsync(ShipmentModeVM model);
        Task<bool> DeleteAsync(int id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<CfDefExpenseType, ShipmentModeVM>.PaginationResult<ShipmentModeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "",
        string sortColumn = "ExpenseTypeID", string sortOrder = "desc");
        Task<string> GetLastUnitAsync();
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsDuplicateAsync(ShipmentModeVM model);
        Task<List<ShipmentModeVM>> GetAllExpenseTypesAsync();
        Task<List<ShipmentModeVM>> GetShipmentModeDetailsAsync(string transportModeTypeId);
    }
}
