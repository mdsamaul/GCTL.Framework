using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VoucherType;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.VoucherType
{
    public interface IVoucherType
    {
        Task<List<VoucherTypeVM>> GetAllAsync();
        Task<VoucherTypeVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(VoucherTypeVM model);
        Task<bool> UpdateAsync(VoucherTypeVM model);
        Task<bool> DeleteAsync(decimal id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<AccVoucherType, VoucherTypeVM>.PaginationResult<VoucherTypeVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherType_Code", string sortOrder = "desc");
        Task<string> GetLastVoucherTypeCodeAsync();
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        Task<bool> IsDuplicateAsync(VoucherTypeVM model);

        #region Reset Duration Type Dropdown

        Task<List<CommonChoiceVM>> DurationTypeDropdown();

        #endregion
    }
}
