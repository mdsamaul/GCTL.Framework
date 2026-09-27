using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Service.OFRAdjustApproval
{
    public interface IOFRAdjustApproval
    {
        #region Grid Section
        #region Get All Requisition for Bottom Grid
        Task<PaginationResponse<OFRAdjustApprovalBottomGridVM>> AllRequisitionforBottomGrid(PaginationRequest request);
        #endregion

        #region Get All Requisition Entry in Top Grid
        Task<PaginationResponse<OFRAdjustApprovalTopGridVM>> GetAllRequisition(PaginationRequest request, string customerid = "", string shipmentmodeid = "");
        #endregion
        #endregion

        #region When User check on radio button
        Task<OFRAdjustApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo);
        Task<OFRAdjustApprovalMasterVM> GetRequisitionMasterByOFRNo(string ofrNo);
        Task<bool> CopyDetailsToTmp(string jobNo, string? currentUserId, BaseViewModel model);
        Task<bool> CopyDetailsToTmpByOFRNo(string ofrNo, string? currentUserId, BaseViewModel model);
        #endregion

        #region Get All Tmp Details
        Task<List<OFRAdjustApprovalDetailsVM>> GetDetailsWithCashBank(string jobno, string? userId);
        #endregion

        #region Approval Adjust Amount Update
        Task<bool> SaveAdjustApprovalAsync(List<OFRAdjustApprovalDetailsSaveVM> model, string? Approveduser);
        #endregion

        #region Dropdown
        Task<IEnumerable<SelectListItem>> GetBankAccount(string value);
        #endregion
    }
}

