using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.EmployeeContactInfos;
using GCTL.Data.Models;

namespace GCTL.Service.EmployeeContactInfos
{
    public interface IEmployeeContactInfoService
    {
        Task<List<EmployeeContactInfosSetupViewModel>> GetAllAsync();
        Task<List<EmployeeContactInfosSetupViewModel>> GetByEmployeeAsync(string employeeId);
        HrmEmployeeContactInfo GetLeaveType(string code);
        Task<EmployeeContactInfosSetupViewModel> GetByIdAsync(string id);
        Task<bool> SaveAsync(EmployeeContactInfosSetupViewModel entityVM, string CompanyCode);
        Task<bool> UpdateAsync(EmployeeContactInfosSetupViewModel entityVM);
        Task<bool> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<EmployeeContactInfosSetupViewModel> GetContactInfoByEmployeeId(string employeeId);
        Task<List<EmployeeContactInfosSetupViewModel>> GetComapnyByBranchCode(string companyCode);
        Task<List<EmployeeContactInfosSetupViewModel>> GetComapnyByCode(string companyCode);

        Task<bool> IsExistByAsync(string code, string EmpContactId);
        Task<string> GenerateNextCode();
        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
