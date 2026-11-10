using GCTL.Core.ViewModels.EditUserVM;

namespace GCTL.Service.UserEditEntry
{
    public interface IUserEditService
    {
        Task<(bool isSuccess, string message, object? data)> SaveAsync(EditUserSetupViewModel model);
        Task<(List<EditUserGridViewModel> Data, int totalRecord, int currentRecord)> GetPaginatedDataAsync(string searchValue, int page, int pageSize, string sortColumn, string sortDirection, string userName);
        Task<EditUserSetupViewModel?> GetByIdAsync(int id);
        Task<EditUserSetupViewModel?> GetByIdAsync(string userName);
    }
}

