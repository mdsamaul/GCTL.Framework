using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
using GCTL.Service.Pagination;

namespace GCTL.Service.OFRAdjustApproval
{
    public interface IOFRAdjustApproval
    {
        #region Grid Section
        #region Get All Requisition for Bottom Grid
        Task<PaginationService<OFRAdjustApprovalBottomGridVM, OFRAdjustApprovalBottomGridVM>.PaginationResult<OFRAdjustApprovalBottomGridVM>> AllRequisitionforBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc");
        #endregion

        #region Get All Requisition Entry in Top Grid
            Task<PaginationService<OFRAdjustApprovalTopGridVM, OFRAdjustApprovalTopGridVM>.PaginationResult<OFRAdjustApprovalTopGridVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "");
        #endregion
        #endregion

        #region When User check on radion button
        Task<OFRAdjustApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo);
        Task<bool> CopyDetailsToTmp(string jobNo, int? currentUserId, BaseViewModel model);
        #endregion

        #region Get All Tmp Details
        //Task<List<OFRAdjustApprovalDetailsVM>> GetAllTmpAsync(int? currentUser);
        Task<List<OFRAdjustApprovalDetailsVM>> GetDetailsWithCashBank(string jobno, int? userId);
        #endregion

        #region Approval  Adjust Amount Update
        Task<bool> SaveAdjustApprovalAsync(List<OFRAdjustApprovalDetailsSaveVM> model, int? Approveduser);
        #endregion
    }
}
