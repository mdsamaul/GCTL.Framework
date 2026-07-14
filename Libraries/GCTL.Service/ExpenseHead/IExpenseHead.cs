using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.ExpenseHead;
using GCTL.Data.Models;
using GCTL.Service.Pagination;

namespace GCTL.Service.ExpenseHead
{
    public interface IExpenseHead
    {
        Task<List<ExpenseHeadVM>> GetAllAsync();
        Task<ExpenseHeadVM> GetByIdAsync(int id);
        Task<bool> SaveAsync(ExpenseHeadVM model);
        Task<bool> UpdateAsync(ExpenseHeadVM model);
        //Task<bool> DeleteAsync(int id);
        Task<bool> IsExistAsync(int id);
        Task<PaginationService<CfDefExpenseHead, ExpenseHeadVM>.PaginationResult<ExpenseHeadVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "",
        string sortColumn = "ExpenseHeadID", string sortOrder = "desc", string expenseType = "", string isReceiptable = "", string serviceType = "");
        Task<string> GetLastExpenseHeadAsync();
        Task<bool> BulkDeleteAsync(List<int> ids);
        Task<bool> IsDuplicateAsync(ExpenseHeadVM model);

        Task<int> GetNextSerialNoAsync(string expenseType, string serviceType, string isReceiptable, int? requestedSerial = null);

        #region Report
        Task<List<ExpenseHeadReportVM>> GetExpenseHeadReportAsync(string expenseTypeId = "");
        Task<byte[]> GenerateExcelReportAsync(string expenseTypeId = "");
        Task<byte[]> GeneratePdfReportAsync(string expenseTypeId = "");

        #endregion
    }
}
