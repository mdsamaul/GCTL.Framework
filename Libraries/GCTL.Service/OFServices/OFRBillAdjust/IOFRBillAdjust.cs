using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Core.ViewModels.OFRSection.OFRBillAdjust;
using GCTL.Service.Pagination;

namespace GCTL.Service.OFRBillAdjust
{
    public interface IOFRBillAdjust
    {
        #region When User Checked on radion then fill input fields and Detaisl data Copied to Tmp Details
        #region Get Requisition Info in Input fields
        Task<OFRBillAdjustMasterVM> GetRequisitionMasterByJobNo(string jobNo);
        Task<OFRBillAdjustMasterVM> GetRequisitionMasterByOFRNo(string ofrNo);
        #endregion
        #region Get Requisition Details into Tmp Details
        Task<bool> CopyDetailsToTmp(string jobNo, string? currentUserId, BaseViewModel model);
        Task<bool> CopyDetailsToTmpByOFRNo(string ofrNo, string? currentUserId, BaseViewModel model);
        #endregion
        #endregion

        #region Get Tmp Details All
        Task<List<OFRBillAdjustDetailsVM>> GetAllTmpAsync(string jobno, string? currentUser);
        #endregion

        #region Approval Bill Adjust Amount Update
        Task<bool> SaveApprovalBillAdjustAsync(List<OFRBillAdjustDetailsSaveVM> model, string? Approveduser);
        #endregion

        #region Get All Requisition for Bottom Grid
        Task<PaginationResponse<OFRBillAdjustBottomGridVM>> GetBottomGrid(PaginationRequest request);
        #endregion

        #region Get All Job Requisition Entry in Top Grid
        Task<PaginationResponse<OFRApprovalTopGridVM>> GetAllRequisition(PaginationRequest request, string customerid = "", string shipmentmodeid = "");
        #endregion
    }
}
