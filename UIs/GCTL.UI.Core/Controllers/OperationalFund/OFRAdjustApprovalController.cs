using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
using GCTL.Service.OFRAdjustApproval;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class OFRAdjustApprovalController : BaseController
    {
        #region Service
        private readonly IOFRAdjustApproval _service;
        public OFRAdjustApprovalController(IOFRAdjustApproval service)
        {
            _service = service;
        }
        #endregion

        #region View Page
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region Grid Section
        #region Top Grid
        [HttpPost]
        public async Task<IActionResult> GetAllRequisitionTopGrid(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "JobDate",
            string sortOrder = "desc",
            string customerid = "",
            string shipmentmodeid = "")
        {
            try
            {
                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortColumn = sortColumn,
                    SortOrder = sortOrder
                };

                var result = await _service.GetAllRequisition(request, customerid, shipmentmodeid);

                bool hasData = result.Data != null && result.Data.Any();

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<OFRAdjustApprovalTopGridVM>(),
                    recordsTotal = result.TotalCount,
                    recordsFiltered = hasData ? result.FilteredCount : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }
        #endregion

        #region Bottom Grid
        [HttpPost]
        public async Task<IActionResult> GetAllRequisitionForBottomGrid(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "JobNo",
            string sortOrder = "desc")
        {
            try
            {
                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortColumn = sortColumn,
                    SortOrder = sortOrder
                };

                var result = await _service.AllRequisitionforBottomGrid(request);

                bool hasData = result.Data != null && result.Data.Any();

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<OFRAdjustApprovalBottomGridVM>(),
                    recordsTotal = result.TotalCount,
                    recordsFiltered = hasData ? result.FilteredCount : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }
        #endregion
        #endregion

        #region When Click Radio Button Then Load Data Input field
        [HttpGet]
        public async Task<IActionResult> JobDetails(string jobno, BaseViewModel model)
        {
            try
            {
                string? currentUserID = LoginInfo?.Username;

                // Get master data
                var masterData = await _service.GetRequisitionMasterByJobNo(jobno);

                if (masterData == null)
                {
                    return Json(new { success = false, message = "No requisition found for this job number" });
                }

                // Copy details to temp table
                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, currentUserID, model);

                if (!detailsCopied)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Master data found but no details available",
                        masterData = masterData
                    });
                }

                return Json(new
                {
                    success = true,
                    masterData = masterData,
                    message = "Data loaded successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #region Get All Tmp Details
        [HttpGet]
        public async Task<IActionResult> LoadTmpTable(string jobNo)
        {
            try
            {
                string? currentUser = LoginInfo?.Username;
                var data = await _service.GetDetailsWithCashBank(jobNo, currentUser);

                if (data == null) data = new List<OFRAdjustApprovalDetailsVM>();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
            }
        }
        #endregion

        #region Bank Account Dropdown
        [HttpGet]
        public async Task<IActionResult> BankAccountDD(string value)
        {
            var data = await _service.GetBankAccount(value);
            return Json(data);
        }
        #endregion

        #region Adjust Approve Amount
        [HttpPost]
        public async Task<IActionResult> SaveAdjustApproval([FromBody] List<OFRAdjustApprovalDetailsSaveVM> model)
        {
            string? Approveduser = LoginInfo?.Username;
            if (model == null || !model.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No approval data received"
                });
            }

            var result = await _service.SaveAdjustApprovalAsync(model, Approveduser);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Approve Failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Approved Successfully"
            });
        }
        #endregion

        #region When Click Edit Button Then Load Data Input field
        [HttpGet]
        public async Task<IActionResult> EditDetails(string jobno, BaseViewModel model)
        {
            try
            {
                string? currentUserID = LoginInfo?.Username;

                // Get master data
                var masterData = await _service.GetRequisitionMasterByJobNo(jobno);

                if (masterData == null)
                {
                    return Json(new { success = false, message = "No requisition found for this job no" });
                }

                // Copy details to temp table
                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, currentUserID, model);

                if (!detailsCopied)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Master data found but no details available",
                        masterData = masterData
                    });
                }

                return Json(new
                {
                    success = true,
                    masterData = masterData,
                    message = "Data loaded successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}

