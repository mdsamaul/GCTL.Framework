using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Designations;
using GCTL.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Designations
{
    public interface IDesignationService
    {
        Task<List<DesignationSetupViewModel>> GetAllAsync();
        Task<DesignationSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(DesignationSetupViewModel entityVM);
        Task<bool> UpdateAsync(DesignationSetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);

        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<IEnumerable<CommonSelectModel>> SelectionDesignationTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);

        // Legacy methods
        List<HrmDefDesignation> GetDesignations();
        HrmDefDesignation GetDesignation(string code); 
        Task<(bool success, bool refSuccess, string message)> DeleteDesignationAsync(List<string> ids, DeleteHistoryViewModel model);
        HrmDefDesignation SaveDesignation(HrmDefDesignation entity);
        bool IsDesignationExistByCode(string code);
        bool IsDesignationExist(string name);
        bool IsDesignationExist(string name, string typeCode);
        IEnumerable<CommonSelectModel> DesignationSelection();
        bool SavePermission(string accessCode);
        bool UpdatePermission(string accessCode);
        bool DeletePermission(string accessCode);
    }
}