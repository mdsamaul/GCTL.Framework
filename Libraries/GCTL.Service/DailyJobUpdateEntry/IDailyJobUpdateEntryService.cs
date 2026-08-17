using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.DailyJobUpdateEntry;
using GCTL.Core.ViewModels.Update;
using GCTL.Service.SepDocumentation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.DailyJobUpdateEntry
{
    public interface IDailyJobUpdateEntryService
    {
        Task<ShipmentUpdateViewModel> GetJobBottomDetailsAsync(string jobNo, decimal id);
        Task<JobDetailDto> GetJobDetailAsync(int id);
        //Task<SepPagedResult<JobDto>> GetJobsPagedAsync(int page, int pageSize, string? customerId, string? shipmentMode, string? dateFrom, string? dateTo, string? search);
        Task<SepPagedResult<JobDto>> GetJobsPagedAsync(
    int page,
    int pageSize,
    string? customerId,
    string? shipmentMode,
    DateTime? dateFrom,
    DateTime? dateTo,
    string? search);
        Task<SepPagedResult<JobStatusDto>> GetJobStatusesPagedAsync(int page, int pageSize, string? search, string? customerCode, DateTime? fromDate, DateTime? toDate);
        Task<JobDetailDto> GetJobTopDetailsAsync(string jobNo);
        Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, string? company);
        Task<CommonReturnViewModel> DeleteJobs(List<decimal> ids);
        Task<List<TimelineStepDto>> GetJobTimelineAsync(string jobNo);
    }
}
