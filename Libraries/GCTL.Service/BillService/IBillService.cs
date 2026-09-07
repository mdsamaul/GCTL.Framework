using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Core.ViewModels.ClearAndF.Update;

namespace GCTL.Service.BillService
{
    public interface IBillService
    {
        Task<PaginationResponse<JobDto>> GetBillPagedAsync(PaginationRequest request, string? customerName, string? shipmentMode, string? dateFrom, string? dateTo);
        Task<BillEntryJobVM> GetJobDetailAsync(decimal id);
        Task<List<ExpenseDetailDto>> GetExpenseDetailsAsync(decimal id);
        Task<List<ChargeTableItemDto>> GetChargeTableDataAsync(decimal id);
        Task<CommonReturnViewModel> AddCostAsync(AddCostRequestDto request, UserInfoViewModel? userInfo);
        Task<CommonReturnViewModel> DeleteChargeAsync(decimal tc, UserInfoViewModel? userInfo);
        Task<PaginationResponse<BillHistoryDto>> GetBillHistoryPagedAsync(PaginationRequest request);
        Task<BillEntryJobVM?> GetBillDetailAsync(string billNo);
        Task<CommonReturnViewModel> DeleteBillsAsync(List<string> billNos, UserInfoViewModel? userInfo);
    }
}