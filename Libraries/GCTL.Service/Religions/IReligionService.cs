using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Religions;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Religions
{
    public interface IReligionService
    {
        Task<List<ReligionSetupViewModel>> GetAllAsync();
        Task<ReligionSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(ReligionSetupViewModel entityVM);
        Task<bool> UpdateAsync(ReligionSetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<IEnumerable<CommonSelectModel>> SelectionReligionTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy methods
        List<HrmDefReligion> GetReligions();
        HrmDefReligion GetReligion(string code);
        Task<(bool success, bool refSuccess, string message)> DeleteReligion(List<string> ids, DeleteHistoryViewModel model);
        HrmDefReligion SaveReligion(HrmDefReligion entity);
        bool IsReligionExistByCode(string code);
        bool IsReligionExist(string name);
        bool IsReligionExist(string name, string typeCode);
        IEnumerable<CommonSelectModel> ReligionSelection();
        bool SavePermission(string accessCode);
        bool UpdatePermission(string accessCode);
        bool DeletePermission(string accessCode);
    }
}