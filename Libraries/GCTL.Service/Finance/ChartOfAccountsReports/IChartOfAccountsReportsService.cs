using GCTL.Core.ViewModels.Finance.ChartOfAccountsReports;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccountsReports
{
    public interface IChartOfAccountsReportsService
    {
        Task<ChartOfAccountsReportResponseVM> GetReportDataAsync(ChartOfAccountsReportFilterVM filter);
        Task<byte[]> ExportExcelAsync(ChartOfAccountsReportFilterVM filter);
        Task<byte[]> ExportPdfAsync(ChartOfAccountsReportFilterVM filter);
        Task<byte[]> ExportCsvAsync(ChartOfAccountsReportFilterVM filter);
    }
}
