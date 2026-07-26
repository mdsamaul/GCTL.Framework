using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.OFRSection.OperationalFund;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.Service.OperationalFund
{
    public interface Ioperational
    {
        #region Dropdown

        Task<IEnumerable<SelectListItem>> GetAllAccountsHead(string serviceTypeId);
        Task<IEnumerable<SelectListItem>> GetAllServiceType();
        Task<IEnumerable<SelectListItem>> GetAllCustomer();
        Task<IEnumerable<SelectListItem>> GetAllShipmentMode();
        Task<IEnumerable<SelectListItem>> GetAllCurencies();

        #endregion


        #region Job Entry List
        Task<PaginationService<JobEntryListVM, JobEntryListVM>.PaginationResult<JobEntryListVM>> GetAllJonEntryList(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "");
        Task<PaginationResponse<JobEntryListVM>> GetAllJonEntryList(PaginationRequest request, string customerid = "", string shipmentmodeid = "");
        #endregion

        #region Get Requisition Info
        Task<RequisitionHeaderVM> GetRequisitionHeaderAsync(string jobno);
        #endregion

        #region Generate Requisition No
        Task<string> GenerateRequisitionNoAsync(string jobNo);
        #endregion

        #region Get All Requisition
        Task<PaginationService<RequisitionVM, RequisitionVM>.PaginationResult<RequisitionVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "OFRNo", string sortOrder = "desc");
        Task<PaginationResponse<RequisitionVM>> GetAllRequisition(PaginationRequest request);
        #endregion

        #region TMp Save & Update Section
        Task<bool> TmpDetailsSaveAsync(RequistionDetailsTmpVM model);
        Task<bool> DeleteTmpDetailAsync(decimal tc, string? currentUserId);
        Task<List<RequistionDetailsTmpVM>> GetAllTmpAsync(string? currentUser);
        #endregion

        #region Master Section
        #region Master & Detail Saved
        Task<bool> MasterDetailsSaveAsync(RequsitionMasterVM model);
        #endregion

        #region Bulk Delete of Master & Details Table
        Task<bool> BulkDeleteAsync(List<string> ofrNos);
        #endregion
        Task<RequisitionHeaderVM> GetRequisitionDetails(string offrNo);
        Task<bool> CopyDetailsToTmp(string offrNo, string? currentUserId);
        Task<bool> ClearTmp(string? currentUserId);
        #endregion
    }
}
