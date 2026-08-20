using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.DailyJobUpdateEntry;
using GCTL.Core.ViewModels.Update;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Update
{
    public interface IJobService
    {
        /// <summary>
        /// Get paginated list of jobs (top table)
        /// Filters out completed jobs
        /// </summary>
        Task<SepPagedResult<JobDto>> GetJobsPagedAsync(
            int page = 1,
            int pageSize = 5,
            string customerId = null,
            string shipmentMode = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string search = null);

        /// <summary>
        /// Get detailed job information by ID
        /// </summary>
        Task<JobDetailDto> GetJobDetailAsync(int id);

        /// <summary>
        /// Get detailed job information by Job Number
        /// </summary>
        Task<JobDetailDto> GetJobTopDetailsAsync(string jobNo);

        /// <summary>
        /// Get paginated list of job statuses (bottom table)
        /// Shows all shipment status updates for jobs
        /// </summary>
        Task<SepPagedResult<JobStatusDto>> GetJobStatusesPagedAsync(
            int page = 1,
            int pageSize = 10,
            string search = null,
            string customerCode = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Get job's shipment update record details (bottom form fields)
        /// </summary>
        Task<ShipmentUpdateViewModel> GetJobBottomDetailsAsync(string jobNo, decimal id);

        /// <summary>
        /// Save or update shipment/job documentation
        /// Creates new SepDocumentation record or updates existing one
        /// Also creates/updates CfDailyJobUpdateEntry for status tracking
        /// </summary>
        Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, string company = null);

        /// <summary>
        /// Delete shipment update records by IDs
        /// Validates deletion rules (can only delete from last status backwards)
        /// </summary>
        Task<CommonReturnViewModel> DeleteJobs(List<decimal> ids);

        /// <summary>
        /// Get shipment timeline for visual display
        /// Returns completed, active, and pending stages
        /// </summary>
        Task<List<TimelineStepDto>> GetJobTimelineAsync(string jobNo);
    }
}