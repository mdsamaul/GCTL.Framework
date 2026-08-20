using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.CustomerRelationshipManagement.AddSalesDeliveryLocation
{
    public interface ISalesDeliveryLocationService
    {
        Task<List<SalesDeliveryLocationViewModel>> GetAllAsync();

        Task<SalesDeliveryLocationViewModel> GetByIdAsync(string id);

        Task<bool> SaveAsyncDeliveryLocation(SalesDeliveryLocationViewModel model);

        Task<bool> UpdateAsyncDeliveryLocation(SalesDeliveryLocationViewModel model);

        Task<bool> DeleteAsyncDeliveryLocation(string deliveryId);

        Task<bool> BulkDeleteAsync(List<string> deliveryIds);

        Task<bool> IsExistAsync(string deliveryId);

        Task<PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>> GetPaginatedDeliveryLocations(
            string customerId,
            int pageNumber = 1,
            int pageSize = 5,
            string searchTerm = "",
            string sortColumn = "DeliveryLocationCode",
            string sortOrder = "asc");

        Task<string> GenerateDeliveryLocationIdAsync();

        Task<List<SalesDeliveryLocationViewModel>> GetDeliveryLocationByCustomer(string cusId);


    }
}
