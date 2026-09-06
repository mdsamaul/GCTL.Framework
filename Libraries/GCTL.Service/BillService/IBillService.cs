using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Core.ViewModels.ClearAndF.Update;

namespace GCTL.Service.BillService
{
    public interface IBillService
    {
        Task<SepPagedResult<JobDto>> GetBillPagedAsync(int page, int pageSize, string? customerName, string? shipmentMode, string? dateFrom, string? dateTo, string? search);
        Task<BillEntryJobVM> GetJobDetailAsync(decimal id);
        Task<List<ExpenseDetailDto>> GetExpenseDetailsAsync(decimal id);
        Task<List<ChargeTableItemDto>> GetChargeTableDataAsync(decimal id);
        Task<CommonReturnViewModel> AddCostAsync(AddCostRequestDto request, UserInfoViewModel? userInfo);
        Task<CommonReturnViewModel> DeleteChargeAsync(decimal tc, UserInfoViewModel? userInfo);
        Task<SepPagedResult<BillHistoryDto>> GetBillHistoryPagedAsync(int page, int pageSize, string? search);
    }
}