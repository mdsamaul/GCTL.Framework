using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.VoucherApproval;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.VoucherApproval
{
    public interface IVoucherApprovalService
    {
        Task<List<CommonSelectVM>> GetCustomerDropdownAsync();
        Task<(List<VoucherApprovalItemVM> Items, int TotalCount, int FilteredCount)> GetApprovalListPagedAsync(VoucherApprovalFilterVM filter);
        Task<VoucherApprovalSummaryVM> GetSummaryAsync(VoucherApprovalFilterVM filter);
        Task<bool> UpdateApprovalStatusAsync(List<string> voucherNos, string status, string username);
    }
}
