using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.SepDocumentation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.SepDocumentation
{
    public interface ISepDocumentationService
    {
        // --- Grid / detail ---
        Task<SepDocPagedResult<SepDocJobListDto>> GetJobsPagedAsync(
            int page, int pageSize, string customerId, string shipmentMode,
            System.DateTime? dateFrom, System.DateTime? dateTo, string search);

        Task<SepDocumentationEntryViewModel> GetJobDetailAsync(decimal tc);

        Task<SepDocumentationEntryViewModel> GetJobDetailByJobNoAsync(string jobNo);

        // --- Save / delete ---
        Task<CommonReturnViewModel> SaveDocumentationAsync(SepDocumentationEntryViewModel model, string company, string loginUser);

        Task<CommonReturnViewModel> DeleteDocumentationAsync(List<decimal> ids);

        // --- Transport mode helper (Sea/Air) ---
        Task<string> GetTransportModeTypeAsync(string shipmentModeId);

        // --- Dropdown helpers (existing) ---
        List<SelectListItem> GetCustomers();
        List<SelectListItem> GetAddresses();
        Task<string> GenerateJobNoAsync(string shipmentModeId, string customerId);
        Task<CommonReturnViewModel> GetCustomerInfoAsync(string customerId);
        Task<string> GenerateJobNoWithoutIdAsync();
    }
}