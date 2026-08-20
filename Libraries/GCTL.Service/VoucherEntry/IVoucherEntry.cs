using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VoucherEntry;
using GCTL.Service.Pagination;

namespace GCTL.Service.VoucherEntry
{
    public interface IVoucherEntry
    {

        #region Voucher Entry Save and Update

        Task<bool> SaveAsync(VoucherEntryVM model);
        Task<bool> UpdateAsync(VoucherEntryVM model);

        #endregion


        #region Voucher Entry Delete Single Or More
        Task<bool> BulkDeleteAsync(List<decimal> ids);

        #endregion


        #region Voucher Entry Tmp Details Delete Single Or More
        Task<bool> DeleteAsync(int? ids);

        #endregion


        #region Generate Voucher Entry Code 
        //Task<string> GetLastVoucherEntryCodeNoAsync();

        Task<string> GetLastVoucherNoAsync(string voucherTypeCode);

        #endregion


        #region Gell Voucher Entry

        Task<PaginationService<VoucherEntryVM, VoucherEntryVM>.PaginationResult<VoucherEntryVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherNo", string sortOrder = "desc", DateTime? startDate = null, DateTime? endDate = null);


        #endregion


        #region Get Master Voucher Information with specific Master ID
        Task<VoucherEntryVM> GetByMasterIdAsync(decimal id);
        #endregion


        #region Get Details Voucher Information with specific Details ID
        Task<List<VoucherDetailsVM>> GetDetailsByVoucherIdAsync(decimal? voucherEntryautoId);
        #endregion


        #region When Edit Button Clicked then Details table data Copied to Tmp Table

        Task<bool> DetailsCopiedToTmp(decimal id, int? currenUserID);

        #endregion


        #region Checking Debit & Credit Amount Is Equal or Not
        Task<bool> IsDebitCreditEqualAsync(string userId);
        #endregion


        #region Get Exist ID
        Task<bool> IsExistAsync(int id);
        #endregion


        #region Company & Branch & General Ledger Dropdown
        Task<List<CommonChoiceVM>> GetVoucherTypeDropdownInfo();

        #endregion
    }
}
