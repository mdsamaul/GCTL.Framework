using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentReceiveEntry;

namespace GCTL.Service.PaymentReceiveEntry
{
    public interface IPaymentReceiveEntryService
    {
        // Stored Procedures
        Task<List<CustomerPaymentListViewModel>> GetCustomerListForPaymentReceiveAsync();
        Task<List<CustomerDueInvoiceViewModel>> GetCustomerDueInvoiceListAsync(string customerId);
        Task<List<PaymentReceivedGridItemViewModel>> GetGridPaymentReceivedAsync(DateTime fromDate, DateTime toDate);
        Task<PaymentHistorySummaryViewModel?> GetPaymentHistorySummaryAsync(string customerCode);
        Task<List<PaymentListItemViewModel>> GetPaymentListAsync(string customerCode, DateTime fromDate, DateTime toDate, int pageIndex = 1, int pageSize = 10);

        // CRUD & Data
        Task<List<PaymentReceiveEntrySetupViewModel>> GetAllAsync();
        Task<PaymentReceiveEntrySetupViewModel?> GetByIdAsync(string code);
        Task<bool> SaveAsync(PaymentReceiveEntrySetupViewModel entityVM);
        Task<bool> UpdateAsync(PaymentReceiveEntrySetupViewModel entityVM);
        Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model);
        Task<bool> IsExistAsync(string code);

        // Sequence Number Generators
        Task<string> GetNextPaymentReceiveNoAsync();
        Task<string> GetNextMrNoAsync();

        // Permissions
        Task<bool> PagePermissionAsync(string accessCode);
        Task<bool> SavePermissionAsync(string accessCode);
        Task<bool> UpdatePermissionAsync(string accessCode);
        Task<bool> DeletePermissionAsync(string accessCode);
    }
}
