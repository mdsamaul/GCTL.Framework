using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Departments;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Departments
{
    public interface IDepartmentService
    {
        Task<List<DepartmentSetupViewModel>> GetAllAsync();
        Task<DepartmentSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(DepartmentSetupViewModel entityVM);
        Task<bool> UpdateAsync(DepartmentSetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<IEnumerable<CommonSelectModel>> SelectionDepartmentTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy methods
        List<HrmDefDepartment> GetDepartments();
        HrmDefDepartment GetDepartment(string code);
        Task<(bool success, bool refSuccess, string message)> DeleteDepartment(List<string> ids, DeleteHistoryViewModel model);
        HrmDefDepartment SaveDepartment(HrmDefDepartment entity);
        bool IsDepartmentExistByCode(string code);
        bool IsDepartmentExist(string name);
        bool IsDepartmentExist(string name, string typeCode);
        IEnumerable<CommonSelectModel> DepartmentSelection();
        bool SavePermission(string accessCode);
        bool UpdatePermission(string accessCode);
        bool DeletePermission(string accessCode);
    }
}