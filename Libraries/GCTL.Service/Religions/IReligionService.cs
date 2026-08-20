using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;

namespace GCTL.Service.Religions
{
    public interface IReligionService
    {
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