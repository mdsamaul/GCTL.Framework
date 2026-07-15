using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.SepDocumentation;
using GCTL.Core.ViewModels.Update;
using GCTL.Core.ViewModels.Update;

namespace GCTL.Service.Update
{
    public interface IJobService
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
        Task<SepPagedResult<JobStatusDto>> GetJobStatusesPagedAsync(int page,int pageSize,string? search,string? customerCode,DateTime? fromDate,DateTime? toDate);
        Task<JobDetailDto> GetJobTopDetailsAsync(string jobNo);
        Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, int? company);
        Task<CommonReturnViewModel> DeleteJobs(List<decimal> ids);
        Task<List<TimelineStepDto>> GetJobTimelineAsync(string jobNo);
    }
}