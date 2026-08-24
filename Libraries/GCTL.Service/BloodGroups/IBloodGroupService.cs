using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.BloodGroups;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.BloodGroups
{
    public interface IBloodGroupService
    {
        Task<List<BloodGroupSetupViewModel>> GetAllAsync();
        Task<BloodGroupSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(BloodGroupSetupViewModel entityVM);
        Task<bool> UpdateAsync(BloodGroupSetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<IEnumerable<CommonSelectModel>> SelectionBloodGroupTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy methods
        List<HrmDefBloodGroup> GetBloodGroups();
        HrmDefBloodGroup GetBloodGroup(string code);
        HrmDefBloodGroup GetBloodGroup(decimal id);
        Task<(bool success, bool refSuccess, string message)> DeleteBloodGroup(List<string> ids, DeleteHistoryViewModel model);
        HrmDefBloodGroup SaveBloodGroup(HrmDefBloodGroup entity);
        bool IsBloodGroupExistByCode(string code);
        bool IsBloodGroupExist(string name);
        bool IsBloodGroupExist(string name, string typeCode);
        bool IsBloodGroupExist(string name, decimal autoId);
        IEnumerable<CommonSelectModel> BloodGroupSelection();
        bool SavePermission(string accessCode);
        bool UpdatePermission(string accessCode);
        bool DeletePermission(string accessCode);
    }
}