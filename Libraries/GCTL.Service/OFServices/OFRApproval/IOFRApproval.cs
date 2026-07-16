using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Service.OFRApproval
{
    public interface IOFRApproval
    {
        #region Get All Job Requisition Entry in Top Grid
        Task<PaginationService<OFRApprovalTopGridVM, OFRApprovalTopGridVM>.PaginationResult<OFRApprovalTopGridVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "");
        #endregion

        #region Get Requisition Info in Input fields
        Task<OFRApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo);
        Task<bool> CopyDetailsToTmp(string jobNo, int? currentUserId);
        //Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(int? currentUser);
        Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(string jobno, int? userId);
        #endregion

        #region Bank Account Dropdown
        Task<IEnumerable<SelectListItem>> GetBankAccount(string value);
        #endregion

        #region Approval Amount Update
        Task<bool> SaveApprovalAsync(List<OFRApprovalDetailsSaveVM> model, int? Approveduser);
        #endregion

        #region Get All Requisition for Top Grid
        Task<PaginationService<OFRApprovalBottomGridVM, OFRApprovalBottomGridVM>.PaginationResult<OFRApprovalBottomGridVM>> AllRequisitionforBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc");
        #endregion

        #region Where User Click on Bottom Grid Edit button
        Task<OFRApprovalMasterVM> GetRequisitionInputData(string jobNo);
        Task<bool> CopyDetailsToTmpTable(string jobNo, int? currentUserId, BaseViewModel model);
        #endregion

    }
}
