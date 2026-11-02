using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.ExpenseHead;
using GCTL.Service.Pagination;

namespace GCTL.Service.ExpenseHead
{
    public interface IExpenseHead
    {
        Task<List<ExpenseHeadVM>> GetAllDataAsync(string expenseType = "", string serviceType = "", string isReceiptable = "", string status = "", string searchTerm = "");
        Task<List<ExpenseHeadVM>> GetAllAsync();
        Task<ExpenseHeadVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(ExpenseHeadVM model);
        Task<bool> UpdateAsync(ExpenseHeadVM model);
        Task<bool> IsExistAsync(int id);
        Task<PaginationResponse<ExpenseHeadVM>> GetPaginatedAsync(PaginationRequest request, string expenseType = "", string isReceiptable = "", string serviceType = "");
        Task<string> GetLastExpenseHeadAsync();
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsDuplicateAsync(ExpenseHeadVM model);
        Task<int> GetNextSerialNoAsync(string expenseType, string serviceType, string isReceiptable, int? requestedSerial = null);
        Task<ExpenseHeadVM?> SaveOrUpdateSpAsync(ExpenseHeadVM model, string? user = null, string? ip = null, string? mac = null);
        Task<ExpenseHeadDropdownsVM> GetDropdownsSpAsync();

        #region Report
        Task<List<ExpenseHeadReportVM>> GetExpenseHeadReportAsync(string expenseTypeId = "", string serviceType = "", string isReceiptable = "", string status = "", string search = "");
        Task<byte[]> GenerateExcelReportAsync(string expenseTypeId = "", string serviceType = "", string isReceiptable = "", string status = "", string search = "");
        Task<byte[]> GeneratePdfReportAsync(string expenseTypeId = "", string serviceType = "", string isReceiptable = "", string status = "", string search = "");
        #endregion
    }
}
