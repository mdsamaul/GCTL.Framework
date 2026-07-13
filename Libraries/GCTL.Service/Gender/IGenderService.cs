using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Gender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.Gender
{
    public interface IGenderService
    {
        Task<List<GenderSetupViewModel>> GetAllAsync();
        Task<GenderSetupViewModel> GetByIdAsync(string code);

        Task<bool> SaveAsync(GenderSetupViewModel entityVM);
        Task<bool> UpdateAsync(GenderSetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
      
        Task<bool> IsExistByCodeAsync(string code);
        Task<bool> IsExistAsync(string name);
        Task<bool> IsExistAsync(string name, string typeCode);
        Task<IEnumerable<CommonSelectModel>> SelectionGenderTypeAsync();

        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
