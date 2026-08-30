using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer
{
    public interface ISalesCustomerService
    {
        Task<List<SalesCustomerViewModel>> GetAllAsync();
        Task<SalesCustomerViewModel> GetByIdAsync(string id);
        Task<bool> SaveAsyncSalesCustomer(SalesCustomerViewModel model);
        Task<bool> UpdateAsyncSalesCustomer(SalesCustomerViewModel model);
        Task<bool> DeleteAsyncSalesCustomer(string customerId);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
        Task<bool> IsExistAsync(string customerId);
        Task<PaginationResponse<SalesCustomerViewModel>> GetPaginatedAsync(PaginationRequest request);
        Task<PaginationService<SalesCustomer, SalesCustomerViewModel>.PaginationResult<SalesCustomerViewModel>> GetPaginatedSalesCustomer(int pageNumber = 1, int pageSize = 5, string searchTerm = "",
        string sortColumn = "FullName", string sortOrder = "asc");
        Task<string> GenerateCustomerIdAsync();
        Task<bool> BulkDeleteAsync(List<string> customerIds);
        Task<List<CoreCountry>> GetCountryDropdownAsync();

        Task<IEnumerable<CommonSelectModel>> SelectionCustomerAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        #region CustomerReport

        Task<List<CustomerReportVM>> GetCustomerReportAsync();

        Task<List<CustomerDeliveryReportVM>> GetCustomerDeliveryReportAsync();

        #endregion
    }
}
