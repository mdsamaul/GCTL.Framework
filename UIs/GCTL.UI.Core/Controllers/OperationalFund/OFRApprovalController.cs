using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Service.OFRApproval;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class OFRApprovalController : BaseController
    {
        #region Service
        private readonly IOFRApproval _service;

        public OFRApprovalController(IOFRApproval service) //: base(translateService, userProfileService)
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
        public async Task<IActionResult> GetAllRequisitionForTopGrid(
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
                var result = await _service.AllRequisitionforBottomGrid(request);
                var hasData = result.Data?.Any() == true;

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<OFRApprovalBottomGridVM>(),
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
        //11241
        //11267

        #region When Click Radion Button Then Load Data Input field
        //[HttpGet]
        //public async Task<IActionResult> GetrequisitionData(string jobno)
        //{
        //    int? currentUserID = await GetCurrentEmployeeIdAsync();

        //    var data = await _service.GetRequisitionMasterByJobNo(jobno);
        //    return Json(data);
        //}
        [HttpGet]
        public async Task<IActionResult> GetRequisitionDataWithDetails(string jobno)
        {
            try
            {
                var masterData = await _service.GetRequisitionMasterByJobNo(jobno);

                if (masterData == null)
                {
                    return Json(new { success = false, message = "No requisition found for this job number" });
                }

                // Copy details to temp table
                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, LoginInfo.Username);

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
        public async Task<IActionResult> GetAllTmpDetails(string jobNo)
        {
            try
            {
                var data = await _service.GetAllTmpAsync(jobNo, LoginInfo.Username);

                if (data == null) data = new List<OFRApprovalDetailsVM>();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
            }
        }

        #endregion

        #region Bank Account Dropdown
        public async Task<IActionResult> BankAccountDD(string value)
        {
            var data = await _service.GetBankAccount(value);
            return Json(data);
        }
        #endregion

        #region Approve Estimate Amount
        [HttpPost]
        public async Task<IActionResult> SaveApproval([FromBody] List<OFRApprovalDetailsSaveVM> model)
        {
            //int? Approveduser = await GetCurrentEmployeeIdAsync();
            if (model == null || !model.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No approval data received"
                });
            }

            var result = await _service.SaveApprovalAsync(model, LoginInfo.Username);

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
                message = "Approved  Successfully"
            });
        }
        #endregion

        #region When Click Edit Button Then Load Data Input field
        [HttpGet]
        public async Task<IActionResult> EditButtonClicked(string ofrNo, string jobno, BaseViewModel model)
        {
            try
            {
                OFRApprovalMasterVM? masterData = null;
                bool detailsCopied = false;

                if (!string.IsNullOrEmpty(ofrNo))
                {
                    masterData = await _service.GetRequisitionInputDataByOFRNo(ofrNo);
                    if (masterData == null)
                    {
                        return Json(new { success = false, message = "No requisition found for this OFR No" });
                    }
                    detailsCopied = await _service.CopyDetailsToTmpTableByOFRNo(ofrNo, LoginInfo.Username, model);
                }
                else if (!string.IsNullOrEmpty(jobno))
                {
                    masterData = await _service.GetRequisitionInputData(jobno);
                    if (masterData == null)
                    {
                        return Json(new { success = false, message = "No requisition found for this job no" });
                    }
                    detailsCopied = await _service.CopyDetailsToTmpTable(jobno, LoginInfo.Username, model);
                }
                else
                {
                    return Json(new { success = false, message = "Invalid request parameters" });
                }

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
