using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.BalanceSheet;

namespace GCTL.Service.Finance.BalanceSheet
{
    public interface IBalanceSheetService
    {
        Task<BalanceSheetReportVM> GetBalanceSheetAsync(BalanceSheetFilterVM filter);
        Task<List<CommonSelectVM>> GetCompaniesAsync();
        Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null);
        Task<byte[]> ExportExcelAsync(BalanceSheetFilterVM filter);
        Task<byte[]> GeneratePdfAsync(BalanceSheetFilterVM filter);
    }
}
