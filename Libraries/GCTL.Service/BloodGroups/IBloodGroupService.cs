using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;

namespace GCTL.Service.BloodGroups
{
    public interface IBloodGroupService
    {
        List<HrmDefBloodGroup> GetBloodGroups();
        HrmDefBloodGroup GetBloodGroup(string code);
        HrmDefBloodGroup GetBloodGroup(decimal id);
        Task<(bool success, bool refSuccess, string message)> DeleteBloodGroup(List<string> ids, DeleteHistoryViewModel model);
        // bool DeleteBloodGroup(string id);    
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