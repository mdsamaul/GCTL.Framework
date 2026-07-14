using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer
{
    public interface ISalesCustomerService
    {
        Task<List<SalesCustomerViewModel>> GetAllAsync();
        Task<SalesCustomerViewModel> GetByIdAsync(string id);
        Task<bool> SaveAsyncSalesCustomer(SalesCustomerViewModel model);
        Task<bool> UpdateAsyncSalesCustomer(SalesCustomerViewModel model);
        Task<bool> DeleteAsyncSalesCustomer(string customerId);
        Task<bool> IsExistAsync(string customerId);
        Task<PaginationService<SalesCustomer, SalesCustomerViewModel>.PaginationResult<SalesCustomerViewModel>> GetPaginatedSalesCustomer(int pageNumber = 1, int pageSize = 5, string searchTerm = "",
        string sortColumn = "FullName", string sortOrder = "asc");
        Task<string> GenerateCustomerIdAsync();
        Task<bool> BulkDeleteAsync(List<string> customerIds);
        Task<List<CoreCompany>> GetCountryDropdownAsync();

        #region CustomerReport

        Task<List<CustomerReportVM>> GetCustomerReportAsync();

        Task<List<CustomerDeliveryReportVM>> GetCustomerDeliveryReportAsync();

        //Task<byte[]> GenerateCustomerExcelReportAsync();
        //Task<byte[]> GenerateCustomerPdfReportAsync();

        #endregion
    }
}
