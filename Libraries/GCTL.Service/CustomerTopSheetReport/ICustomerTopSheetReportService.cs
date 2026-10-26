using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.CustomerTopSheetReport
{
    public interface ICustomerTopSheetReportService
    {
        Task<CustomerTopSheetFilterOptionsDTO> GetFilterOptionsAsync(string? fromDate = null, string? toDate = null, string? companyCode = null, string? branchCode = null);
        Task<List<CommonSelectModel>> GetBranchesAsync(string? companyCode);
        Task<List<string>> GetJobNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate);
        Task<List<string>> GetBillNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate);
        Task<List<CustomerTopSheetCustomerDTO>> GetReportDataAsync(CustomerTopSheetFilter filter);
        Task<CustomerTopSheetGridResult> GetGridDataAsync(CustomerTopSheetFilter filter);
        Task<CustomerTopSheetSummaryDTO> GetSummaryAsync(CustomerTopSheetFilter filter);
        Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(CustomerTopSheetFilter filter, string exportFormat, string printUser = "");
        Task<byte[]> GeneratePdfReportAsync(CustomerTopSheetFilter filter, string printUser = "");
        Task<byte[]> GenerateExcelReportAsync(CustomerTopSheetFilter filter);
        Task<byte[]> GenerateCsvReportAsync(CustomerTopSheetFilter filter);
    }
}
