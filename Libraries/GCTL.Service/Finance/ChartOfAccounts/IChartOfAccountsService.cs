using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.ChartOfAccounts
{
    public interface IChartOfAccountsService
    {
        Task<List<ChartOfAccountsTreeItemVM>> GetTreeAsync(bool showArchived = false, string search = "");
        Task<ChartOfAccountsStatsVM> GetStatsAsync();
        Task<byte[]> ExportCsvAsync();
        Task<CommonReturnViewModel> SeedDefaultChartOfAccountsAsync();
    }
}
