using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.VoucherEntryVM;
using System.Threading.Tasks;

namespace GCTL.Service.Finance.VoucherEntry
{
    public interface IVoucherEntryService
    {
        Task<VoucherEntryListResultVM> GetAllAsync(VoucherEntryFilterVM filter);
        Task<CommonReturnViewModel> GetByIdAsync(string voucherEntryCodeNo);
        Task<CommonReturnViewModel> GetByVoucherNoAsync(string voucherNo);
        Task<CommonReturnViewModel> SaveAsync(VoucherEntryVM model, int? userId = null, string? ip = null, string? mac = null);
        Task<CommonReturnViewModel> DeleteAsync(string voucherEntryCodeNo, int? userId = null);
        Task<CommonReturnViewModel> PostAsync(string voucherEntryCodeNo, int? userId = null);
        Task<VoucherDropdownsVM> GetDropdownsAsync();
        Task<string> GetNextVoucherNoAsync(string voucherTypeCode, string? financialYear = null);
        Task<CommonReturnViewModel> GetVoucherPrintAsync(string voucherEntryCodeNo);
        Task<string> GetUserFullNameAsync(string? userId, string? userName);
        Task<byte[]> GeneratePdfAsync(VoucherEntryFilterVM filter);
    }
}
