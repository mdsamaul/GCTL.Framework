using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;

namespace GCTL.Service.Dashboard
{
    public interface IDashboardBillEntryService
    {
        Task<DashboardBillEntryDTO> GetBillEntryDataAsync(DashboardFilterDTO filter);
    }
}
