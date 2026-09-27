using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.Companies;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;

namespace GCTL.Service.Companies
{
    public interface ICompanyService
    {
        Task<List<CompanySetupViewModel>> GetAllAsync();
        Task<CompanySetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(CompanySetupViewModel entityVM);
        Task<bool> UpdateAsync(CompanySetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy / Sync methods
        List<CoreCompany> GetCompanies();
        CoreCompany GetCompany(string code);
        CompanyModel GetCompanyByCode(string code);
        Task<(bool success, bool refSuccess, string message)> DeleteCompany(List<string> ids, DeleteHistoryViewModel model);
        CoreCompany SaveCompany(CoreCompany entity);
        bool IsCompanyExistByCode(string code);
        bool IsCompanyExist(string name);
        bool IsCompanyExist(string name, string companyCode);
        bool SavePermission(string accessCode);
        bool UpdatePermission(string accessCode);
        bool DeletePermission(string accessCode);
        Task<IEnumerable<CommonSelectModel>> GetCompanyDropDown();
    }
}