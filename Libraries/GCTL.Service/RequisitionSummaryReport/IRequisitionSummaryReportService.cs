using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.RequisitionSummaryReport;

namespace GCTL.Service.RequisitionSummaryReport
{
    public interface IRequisitionSummaryReportService
    {
        Task<(List<RequisitionSummaryReportItemDTO> Data, int TotalCount, int FilteredCount)> GetGridDataAsync(RequisitionSummaryReportFilterVM filter);
        Task<RequisitionSummaryReportSummaryDTO> GetSummaryAsync(RequisitionSummaryReportFilterVM filter);
        Task<RequisitionSummaryReportFilterOptionsDTO> GetFilterOptionsAsync(string? fromDate, string? toDate, string? customerIds = null, string? portIds = null, string? shipmentModes = null);
        Task<byte[]> GeneratePdfAsync(RequisitionSummaryReportFilterVM filter);
    }
}
