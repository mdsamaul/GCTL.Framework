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
        #endregion
        #region Get Requisition Details into Tmp Details
        Task<bool> CopyDetailsToTmp(string jobNo, int? currentUserId, BaseViewModel model);
        #endregion
        #endregion

        #region Get Tmp Details All
        Task<List<OFRBillAdjustDetailsVM>> GetAllTmpAsync(int? currentUser);
        #endregion

        #region Approval Bill Adjust Amount Update
        Task<bool> SaveApprovalBillAdjustAsync(List<OFRBillAdjustDetailsSaveVM> model, int? Approveduser);
        #endregion

        #region Get All Requisition for Bottom Grid
        Task<PaginationService<OFRBillAdjustBottomGridVM, OFRBillAdjustBottomGridVM>.PaginationResult<OFRBillAdjustBottomGridVM>> GetBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc");
        #endregion

        #region Get All Job Requisition Entry in Top Grid
        Task<PaginationService<OFRApprovalTopGridVM, OFRApprovalTopGridVM>.PaginationResult<OFRApprovalTopGridVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "");
        #endregion
    }
}
