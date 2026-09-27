using GCTL.Core.ViewModels;
using GCTL.Service.Pagination;
using GCTL.UI.Core.ViewModels.ShipmentMode;

using GCTL.Core.DataTables;

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
        Task<PaginationResponse<ShipmentStatusVM>> GetPaginatedAsync(PaginationRequest request);
        Task<List<ShipmentStatusVM>> GetShipmentStatusDetailsAsync(string transportModeTypeId);
    }
}