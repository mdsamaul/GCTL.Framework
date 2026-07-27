using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.CfDefPlaceOfLoading;

namespace GCTL.Service.CfDefPlaceOfLoading
{
    public interface ICfDefPlaceOfLoadingService
    {
        #region CRUD

        Task<List<CfDefPlaceOfLoadingSetupViewModel>> GetAllAsync();

        Task<CfDefPlaceOfLoadingSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(CfDefPlaceOfLoadingSetupViewModel entityVM);

        Task<bool> UpdateAsync(CfDefPlaceOfLoadingSetupViewModel entityVM);

        Task<(bool succses, string messege)> DeleteTab(
            List<string> ids,
            DeleteHistoryViewModel model);

        #endregion

        #region Dropdown

        Task<IEnumerable<CommonSelectModel>> SelectionPlaceOfLoadingAsync();

        #endregion

        #region Duplicate Check

        Task<bool> IsExistByCodeAsync(string code);

        Task<bool> IsExistAsync(string name);

        Task<bool> IsExistAsync(string name, string code);

        #endregion

        #region Permission

        Task<bool> PagePermissionAsync(string accessCode);

        Task<bool> SavePermissionAsync(string accessCode);

        Task<bool> UpdatePermissionAsync(string accessCode);

        Task<bool> DeletePermissionAsync(string accessCode);

        #endregion
    }
}