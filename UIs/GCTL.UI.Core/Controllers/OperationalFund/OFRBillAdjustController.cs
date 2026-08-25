using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Core.ViewModels.OFRSection.OFRBillAdjust;
using GCTL.Service.OFRBillAdjust;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class OFRBillAdjustController : BaseController
    {
        #region Service
        private readonly IOFRBillAdjust _service;
        public OFRBillAdjustController(IOFRBillAdjust service) //: base(translateService, userProfileService)
        {
            _service = service;
        }
        #endregion

        #region View page
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region When Click Radion Button Then Load Data Input field

        [HttpGet]
        public async Task<IActionResult> BillAdjustUIDetails(string jobno, BaseViewModel model)
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
        public async Task<IActionResult> TmpDetailsBillAdjustUI(string jobNo)
        {
            try
            {
                string? currentUser = LoginInfo?.Username;
                var data = await _service.GetAllTmpAsync(jobNo, currentUser);

                if (data == null) data = new List<OFRBillAdjustDetailsVM>();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
            }
        }

        #endregion

        #region Approve Bill Adjust  Amount
        [HttpPost]
        public async Task<IActionResult> SaveApprovalBillAdjust([FromBody] List<OFRBillAdjustDetailsSaveVM> model)
        {
            string? Approveduser = LoginInfo?.Username;
            if (model == null || !model.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No Bill Adjust  data received"
                });
            }

            var result = await _service.SaveApprovalBillAdjustAsync(model, Approveduser);

            if (!result)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Bill Adjust  Failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Bill Adjust  Successfully"
            });
        }
        #endregion

        #region Get All Requisition List for Top Grid
        [HttpPost]
        public async Task<IActionResult> GetAllRequisitionList(
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
                var hasData = result.Data?.Any() == true;

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<OFRApprovalTopGridVM>(),
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

        #region Get All Requisition List for Bottom Grid
        [HttpPost]
        public async Task<IActionResult> GetAllRequisitionForBottomGrid(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "OFRNo",
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

                var result = await _service.GetBottomGrid(request);
                var hasData = result.Data?.Any() == true;

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<OFRBillAdjustBottomGridVM>(),
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

        #region When Click Edit Button Then Load Data Input field

        [HttpGet]
        public async Task<IActionResult> EditButtonClicked(string jobno, BaseViewModel model)
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
