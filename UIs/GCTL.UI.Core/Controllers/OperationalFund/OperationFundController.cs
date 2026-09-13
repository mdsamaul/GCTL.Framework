using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.OFRSection.OperationalFund;

using GCTL.Service.OperationalFund;

//using GCTL.Service.UserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    [Authorize]
    public class OperationFundController : BaseController
    {

        #region Service
        private readonly Ioperational _service;
        public OperationFundController
            (/*ITranslateService translateService, IUserProfileService userProfileService, */Ioperational service) //: base(translateService, userProfileService)
        {
            _service = service;
        }
        #endregion


        #region Index View Page
        public IActionResult Index()
        {
            return View();
        }
        #endregion


        #region Job Entry List
        [HttpPost]
        public async Task<IActionResult> GetAllJobList(
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
                var result = await _service.GetAllJonEntryList(request, customerid, shipmentmodeid);
                var hasData = result.Data?.Any() == true;

                return Ok(new
                {
                    draw,
                    data = hasData ? result.Data : new List<JobEntryListVM>(),
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

        #region Get Requisition Header Info
        [HttpGet]
        public async Task<IActionResult> GetHeaderData(string jobno)
        {
            var data = await _service.GetRequisitionHeaderAsync(jobno);
            return Json(data);
        }
        #endregion

        #region Generate Requisition No
        [HttpGet]
        public async Task<IActionResult> GenerateRequisitionNo(string jobNo)
        {
            if (string.IsNullOrEmpty(jobNo)) return BadRequest("JobNo is required");

            var requisitionNo = await _service.GenerateRequisitionNoAsync(jobNo);
            return Ok(new { requisitionNo });
        }
        #endregion

        #region Get All Requisition

        [HttpPost]
        public async Task<IActionResult> GetAllRequisition(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "JobNo",
            string sortOrder = "desc"
            )
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

                var result = await _service.GetAllRequisition(request);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        draw,
                        data = new List<JobEntryListVM>(),
                        recordsTotal = 0,
                        recordsFiltered = 0
                    });
                }

                return Ok(new
                {
                    draw,
                    data = result.Data,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        #endregion


        #region Dropdown Section

        #region Accounts Head Dropdown
        public async Task<IActionResult> GetAccountsHeadDD(string serviceTypeId, string shipmentModeId)
        {
            var headdata = await _service.GetAllAccountsHead(serviceTypeId, shipmentModeId);
            return Json(headdata);
        }
        #endregion

        #region Service Type Dropdown
        public async Task<IActionResult> GetServiceTypeDD()
        {
            var servicedata = await _service.GetAllServiceType();
            return Json(servicedata);
        }
        #endregion

        #region Customer Dropdown
        public async Task<IActionResult> GetEmpDD()
        {
            var emp = await _service.GetAllCustomer();
            return Json(emp);
        }
        #endregion

        #region Shipment Mode
        public async Task<IActionResult> GetShipemtDD()
        {
            var shipdata = await _service.GetAllShipmentMode();
            return Json(shipdata);
        }
        #endregion

        #region Curencies Dropdown
        public async Task<IActionResult> GetCurenciesDD()
        {
            var currencies = await _service.GetAllCurencies();
            return Json(currencies);
        }
        #endregion

        #endregion

        #region TMP Section

        #region Tmp Details Saved
        [HttpPost]
        public async Task<ActionResult> CreateTmpDetails([FromBody] RequistionDetailsTmpVM model)
        {
            try
            {
                if (model == null) return BadRequest(new { message = "Validation Failed." });
                model.ToAudit(LoginInfo);
                var result = await _service.TmpDetailsSaveAsync(model);


                //if (!result.Success) return BadRequest(new { message = "Insertion Failed." });

                return Ok(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }
        #endregion

        #region Delete Tmp Details with current User ID
        [HttpDelete("OperationFund/DeleteTmpDetail/{tc}")]
        public async Task<IActionResult> DeleteTmpDetail(decimal tc)
        {
            try
            {
                string? currentUserId = LoginInfo?.Username;
                var success = await _service.DeleteTmpDetailAsync(tc, currentUserId);

                if (success)
                    return Ok(new { message = "Data Deleted Successfully" });
                else
                    return NotFound(new { message = "Data not found or not authorized" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
        #endregion

        #region When User Click on CLear Button then Current User Related Data Delete from Tmp Details Table
        [HttpDelete]
        public async Task<IActionResult> ClearTmpDetails()
        {
            try
            {
                string? currentUserId = LoginInfo?.Username;// await GetCurrentEmployeeIdAsync();
                var success = await _service.ClearTmp(currentUserId);

                if (success) return Ok(new { message = "Data Deleted Successfully" });

                else return NotFound(new { message = "Data not found or not authorized" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
        #endregion

        #region Get All Tmp Details

        [HttpGet]
        public async Task<IActionResult> GetAllTmpVoucherDetails()
        {
            try
            {
                string? currentUser = LoginInfo.Username; //await GetCurrentEmployeeIdAsync();
                var data = await _service.GetAllTmpAsync(currentUser);

                if (data == null) data = new List<RequistionDetailsTmpVM>();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
            }
        }

        #endregion

        #endregion

        #region Master and Details Section

        #region Master & Details Saved
        [HttpPost]
        public async Task<ActionResult> CreateMasterDetails([FromBody] RequsitionMasterVM model)
        {
            try
            {
                if (model == null) return BadRequest(new { message = "OFFR No  is Required." });
                model.ToAudit(LoginInfo);
                var result = await _service.MasterDetailsSaveAsync(model);

                if (!result) return BadRequest(new { message = "Insertion Failed." });

                return Ok(new { success = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }
        #endregion

        #region Bulk Delete Master & Details
        [HttpPost]
        public async Task<IActionResult> DeleteRequisition([FromBody] List<string> ofrNos)
        {
            if (ofrNos == null || !ofrNos.Any())
                return BadRequest("No data selected");

            var result = await _service.BulkDeleteAsync(ofrNos);

            return result ? Ok() : StatusCode(500, "Delete failed");
        }

        #endregion

        #region Get Master and Details Table Data When User Click on OFFRNo
        [HttpGet]
        public async Task<IActionResult> GetRequisitionDetails(string offrNo)
        {
            if (string.IsNullOrEmpty(offrNo))
                return BadRequest("OFRNo is required");

            var data = await _service.GetRequisitionDetails(offrNo);

            if (data == null)
                return NotFound("Requisition not found");

            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> CopyToTmp([FromBody] string offrNo)
        {
            string? currentUserId = LoginInfo?.Username;
            await _service.CopyDetailsToTmp(offrNo, currentUserId);
            return Ok(new { success = true });
        }
        #endregion

        #endregion
    }
}
