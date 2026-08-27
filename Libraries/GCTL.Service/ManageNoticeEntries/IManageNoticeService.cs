using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ManageNoticeEntries;

namespace GCTL.Service.ManageNoticeEntries
{
    public interface IManageNoticeService
    {
        Task<bool> BulkDeleteAsync(List<int> tcs, DeleteHistoryViewModel model);
        Task<bool> EditAsync(ManageNoticeSetupViewModel model);
        Task<string> GenerateIdAsync();
        Task<ManageNoticeSetupViewModel> GetByIdAsync(int id);
        Task<EmployeeFilterResultDto> GetFilterEmpAsync(EmployeeFilterViewModel filter);
        Task<(List<ManageNoticeSetupViewModel> Data, int TotalRecords, int FilterRecords)> GetPaginatedDataAsync(string searchValue, int page, int pageSize, string sortColumn, string sortDirection);
        Task<bool> SaveAsync(ManageNoticeSetupViewModel model);
        Task<bool> SentNoticeToEmployeeAsync(List<string> empIds, List<int> tcs);


        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
