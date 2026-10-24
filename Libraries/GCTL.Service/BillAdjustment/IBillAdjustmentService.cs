using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ClearAndF.BillAdjustment;

namespace GCTL.Service.BillAdjustment
{
    public interface IBillAdjustmentService
    {
        Task<List<ApprovedBillListDto>> GetApprovedBillsAsync(DateTime? dateFrom, DateTime? dateTo, string? searchTerm = null, string? companyCode = null);
        Task<BillAdjustmentScreenDataResponse?> GetBillDetailsByBillNoAsync(string billNo);
        Task<CommonReturnViewModel> SaveAdjustmentAsync(BillAdjustmentSaveRequestDto model);
        Task<string> GetNextAdjustmentNoAsync(string adjustmentType);
        Task<List<ChcHeadOptionDto>> GetChcHeadsAsync();
    }
}
