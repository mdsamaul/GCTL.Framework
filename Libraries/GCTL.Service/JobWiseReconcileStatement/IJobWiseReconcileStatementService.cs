using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.JobWiseReconcileStatement;

namespace GCTL.Service.JobWiseReconcileStatement
{
    public interface IJobWiseReconcileStatementService
    {
        Task<JobWiseReconcileFilterOptionsVM> GetFilterOptionsAsync();
        Task<List<string>> GetJobNosAsync(string? expenseTypeId, string? customerId, string? fromDate, string? toDate);
        Task<JobWiseReconcileReportResultVM> GetReportDataAsync(JobWiseReconcileStatementFilterVM filter);
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportExcelAsync(JobWiseReconcileStatementFilterVM filter);
    }
}
