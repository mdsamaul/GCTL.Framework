using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.GeneralLedgerReport;

namespace GCTL.Service.Finance.GeneralLedgerReport
{
    public interface IGeneralLedgerReportService
    {
        Task<GeneralLedgerReportVM> GetLedgerReportAsync(GeneralLedgerReportFilterVM filter);
        Task<List<CommonSelectVM>> GetCompaniesAsync();
        Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null);
        Task<List<CommonSelectVM>> GetAccountHeadsAsync();
        Task<byte[]> ExportExcelAsync(GeneralLedgerReportFilterVM filter);
        Task<byte[]> GeneratePdfAsync(GeneralLedgerReportFilterVM filter);
    }
}
