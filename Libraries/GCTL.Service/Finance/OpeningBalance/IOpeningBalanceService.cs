using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.OpeningBalancesVM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.OpeningBalance
{
    public interface IOpeningBalanceService
    {
        Task<OpeningBalanceListResultVM> GetAllAsync(OpeningBalanceFilterVM filter);
        Task<CommonReturnViewModel> GetByIdAsync(string code);
        Task<CommonReturnViewModel> SaveAsync(OpeningBalanceVM model, int? userId = null, string? ip = null, string? mac = null);
        Task<CommonReturnViewModel> DeleteAsync(string code, int? userId = null);
        Task<CommonReturnViewModel> BulkDeleteAsync(List<string> codes, int? userId = null);
        Task<OpeningBalanceDropdownsVM> GetDropdownsAsync();
        Task<string> GetNextCodeAsync(string financialYear);
        Task<CommonReturnViewModel> ImportCsvAsync(List<OpeningBalanceVM> items, int? userId = null, string? ip = null, string? mac = null);
        Task<byte[]> GeneratePdfAsync(OpeningBalanceFilterVM filter);
    }
}
