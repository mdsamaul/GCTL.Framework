using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardIncomeStatementService
    {
        Task<DashboardIncomeStatementDTO> GetIncomeStatementDataAsync(DashboardFilterDTO filter);
        Task<byte[]> GeneratePdfAsync(DashboardFilterDTO filter);
        Task<byte[]> GenerateExcelAsync(DashboardFilterDTO filter);
    }
}

