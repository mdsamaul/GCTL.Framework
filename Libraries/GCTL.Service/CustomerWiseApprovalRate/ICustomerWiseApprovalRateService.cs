using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.CustomerWiseApprovalRate;

namespace GCTL.Service.CustomerWiseApprovalRate
{
    public interface ICustomerWiseApprovalRateService
    {
        Task<string> GetNextCostingIdAsync();
        Task<CustomerWiseApprovalRateViewModel?> GetByCustomerIdAsync(string customerId);
        Task<CommonReturnViewModel> SaveAsync(CustomerWiseApprovalRateViewModel model, UserInfoViewModel? userInfo);
        Task<CommonReturnViewModel> DeleteAsync(string costingId, UserInfoViewModel? userInfo);
        Task<CommonReturnViewModel> DeleteMultipleAsync(List<string> costingIds, UserInfoViewModel? userInfo);
        Task<List<CustomerWiseApprovalRateMasterGridModel>> GetAllMasterListAsync();
        Task<List<CommonSelectModel>> GetCustomersAsync();
        Task<List<CommonSelectModel>> GetChcHeadsAsync();
        Task<List<CommonSelectModel>> GetExpenseTypesAsync();
    }

    public class CommonSelectModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
