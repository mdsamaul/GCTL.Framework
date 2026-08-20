using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Service.OFRApproval
{
    public interface IOFRApproval
    {
        #region Get All Job Requisition Entry in Top Grid
        Task<PaginationResponse<OFRApprovalTopGridVM>> GetAllRequisition(PaginationRequest request, string customerid = "", string shipmentmodeid = "");
        #endregion

        #region Get Requisition Info in Input fields
        Task<OFRApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo);
        Task<bool> CopyDetailsToTmp(string jobNo, string? currentUserId);
        //Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(int? currentUser);
        Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(string jobno, string? userId);
        #endregion

        #region Bank Account Dropdown
        Task<IEnumerable<SelectListItem>> GetBankAccount(string value);
        #endregion

        #region Approval Amount Update
        Task<bool> SaveApprovalAsync(List<OFRApprovalDetailsSaveVM> model, string? Approveduser);
        #endregion

        #region Get All Requisition for Top Grid
        Task<PaginationResponse<OFRApprovalBottomGridVM>> AllRequisitionforBottomGrid(PaginationRequest request);
        #endregion

        #region Where User Click on Bottom Grid Edit button
        Task<OFRApprovalMasterVM> GetRequisitionInputData(string jobNo);
        Task<bool> CopyDetailsToTmpTable(string jobNo, string? currentUserId, BaseViewModel model);
        #endregion

    }
}
