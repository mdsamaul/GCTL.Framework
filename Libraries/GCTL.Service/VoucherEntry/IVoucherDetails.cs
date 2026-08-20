using GCTL.Core.ViewModels.VoucherEntry;

namespace GCTL.Service.VoucherEntry
{
    public interface IVoucherDetails
    {

        #region Get All Tmp Voucher Details
        Task<List<VoucherEntryDetailsTempVM>> GetAllAsync(int? currentUser);
        #endregion


        #region Get Details Voucher Information with specific ID
        Task<VoucherEntryDetailsTempVM> GetByIdAsync(decimal id);
        #endregion


        #region Tmp Details Voucher Entry Save and Update
        Task<bool> SaveAsync(VoucherEntryDetailsTempVM model);
        Task<bool> UpdateAsync(VoucherEntryDetailsTempVM model);
        #endregion


        #region Delete Voucher Details
        Task<bool> DeleteAsync(decimal id);
        Task<bool> BulkDeleteAsync(List<decimal> ids);
        #endregion


        #region Get Exist ID
        Task<bool> IsExistAsync(int id);
        #endregion


        #region Generate Last Voucher Details Code
        Task<string> GetLastVoucherDetailsCodeAsync();
        #endregion


        #region Account Head Dropdown on Event Change Backend
        Task<LedgerDetailsVM> GetAllLedgerByIdAsync(string id);
        #endregion

    }
}
