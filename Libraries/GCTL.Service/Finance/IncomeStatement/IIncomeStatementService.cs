using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.IncomeStatement;

namespace GCTL.Service.Finance.IncomeStatement
{
    public interface IIncomeStatementService
    {
        Task<IncomeStatementReportVM> GetIncomeStatementAsync(IncomeStatementFilterVM filter);
        Task<List<CommonSelectVM>> GetCompaniesAsync();
        Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null);
        Task<byte[]> ExportExcelAsync(IncomeStatementFilterVM filter);
        Task<byte[]> GeneratePdfAsync(IncomeStatementFilterVM filter);
    }
}
