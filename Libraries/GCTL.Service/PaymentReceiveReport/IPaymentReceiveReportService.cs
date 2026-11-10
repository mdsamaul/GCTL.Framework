using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.PaymentReceiveReport;

namespace GCTL.Service.PaymentReceiveReport
{
    public interface IPaymentReceiveReportService
    {
        Task<PaymentReceiveReportViewModel> GetFilterOptionsAsync();
        Task<(List<PaymentReceiveReportItemDTO> Items, PaymentReceiveReportSummaryDTO Summary)> GetGridDataAsync(PaymentReceiveReportFilter filter);
        Task<byte[]> GeneratePdfReportAsync(PaymentReceiveReportFilter filter);
        Task<byte[]> ExportExcelAsync(PaymentReceiveReportFilter filter);
    }
}

