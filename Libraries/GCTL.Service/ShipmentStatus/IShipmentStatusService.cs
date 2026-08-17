using GCTL.Core.ViewModels;
using GCTL.Service.Pagination;
using GCTL.UI.Core.ViewModels.ShipmentMode;

namespace GCTL.Service.ShipmentMode
{
    public interface IShipmentStatusService
    {
        Task<bool> SaveAsync(ShipmentStatusVM model);
        Task<bool> UpdateAsync(ShipmentStatusVM model);
        Task<bool> DeleteAsync(int id);
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsExistAsync(int id);
        Task<bool> IsDuplicateAsync(ShipmentStatusVM model);
        Task<string> GetLastIdAsync();
        Task<ShipmentStatusVM> GetByIdAsync(int id);
        Task<PaginationService<GCTL.Data.Models.CfShipmentStatus, ShipmentStatusVM>.PaginationResult<ShipmentStatusVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "ShipmentStatusId", string sortOrder = "desc");
        Task<List<ShipmentStatusVM>> GetShipmentStatusDetailsAsync(string transportModeTypeId);
    }
}