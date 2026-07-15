using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Data.Models;
using GCTL.Service.Pagination;


namespace GCTL.Service.CustomerRelationshipManagement.AddContactPerson
{
    public interface IContactPersonService
    {
        Task<List<SalesContactPersonViewModel>> GetAllAsync();
        Task<SalesContactPersonViewModel> GetByIdAsync(decimal id);
        Task<bool> SaveAsync(SalesContactPersonViewModel model);
        Task<bool> UpdateAsync(SalesContactPersonViewModel model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(decimal id);
        Task<PaginationService<SalesContactPerson, SalesContactPersonViewModel>.PaginationResult<SalesContactPersonViewModel>> GetPaginatedAsync(int pageNumber = 1, int ContactpageSize = 10, string searchTerm = "", string sortColumn = "Cpid", string sortOrder = "asc");
        Task<string> GetLastCPIDAsync();
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(SalesContactPersonViewModel model);

        //For Contact DropDown
        Task<List<SalesContactPersonViewModel>> DropdownContact();
    }
}
