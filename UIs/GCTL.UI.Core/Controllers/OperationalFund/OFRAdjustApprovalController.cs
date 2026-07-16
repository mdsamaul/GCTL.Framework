//using GCTL.Core.ViewModels;
//using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
//using GCTL.Core.ViewModels.OFRSection.OFRBillAdjust;
//using GCTL.Service.Language;
//using GCTL.Service.OFRAdjustApproval;
//using GCTL.Service.UserProfile;
//using Microsoft.AspNetCore.Mvc;

//namespace GCTL_App.Controllers.OFRAdjustApproval
//{
//    public class OFRAdjustApprovalController : BaseController
//    {
//        #region Service
//        private readonly IOFRAdjustApproval _service;
//        public OFRAdjustApprovalController(ITranslateService translateService, IUserProfileService userProfileService, IOFRAdjustApproval service) : base(translateService, userProfileService)
//        {
//            _service = service;
//        }
//        #endregion

//        #region View Page
//        public IActionResult Index()
//        {
//            return View();
//        }
//        #endregion

//        #region Grid Section
//        #region Top Grid
//        public async Task<IActionResult> GetAllRequisitionTopGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "")
//        {
//            try
//            {
//                var result = await _service.GetAllRequisition(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, customerid, shipmentmodeid);

//                if (result.Data == null || !result.Data.Any())
//                {
//                    return Ok(new
//                    {
//                        Data = new List<OFRBillAdjustBottomGridVM>(),
//                        TotalCount = 0,
//                        PaginationInfo = new
//                        {
//                            StartItem = 0,
//                            EndItem = 0,
//                            TotalItems = 0,
//                            PageNumbers = new List<int>(),
//                            TotalPages = 0,
//                            CurrentPage = 0
//                        },
//                        Message = "No Job Found."
//                    });
//                }

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
//            }
//        }

//        #endregion

//        #region Bottom Grid
//        public async Task<IActionResult> GetAllRequisitionForBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc")
//        {
//            try
//            {
//                var result = await _service.AllRequisitionforBottomGrid(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

//                if (result.Data == null || !result.Data.Any())
//                {
//                    return Ok(new
//                    {
//                        Data = new List<OFRBillAdjustBottomGridVM>(),
//                        TotalCount = 0,
//                        PaginationInfo = new
//                        {
//                            StartItem = 0,
//                            EndItem = 0,
//                            TotalItems = 0,
//                            PageNumbers = new List<int>(),
//                            TotalPages = 0,
//                            CurrentPage = 0
//                        },
//                        Message = "No Job Found."
//                    });
//                }

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
//            }
//        }
//        #endregion
//        #endregion

//        #region When Click Radion Button Then Load Data Input field

//        [HttpGet]
//        public async Task<IActionResult> JobDetails(string jobno, BaseViewModel model)
//        {
//            try
//            {
//                int? currentUserID = await GetCurrentEmployeeIdAsync();

//                // Get master data
//                var masterData = await _service.GetRequisitionMasterByJobNo(jobno);

//                if (masterData == null)
//                {
//                    return Json(new { success = false, message = "No requisition found for this job number" });
//                }

//                // Copy details to temp table
//                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, currentUserID, model);

//                if (!detailsCopied)
//                {
//                    return Json(new
//                    {
//                        success = false,
//                        message = "Master data found but no details available",
//                        masterData = masterData
//                    });
//                }

//                return Json(new
//                {
//                    success = true,
//                    masterData = masterData,
//                    message = "Data loaded successfully"
//                });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }
//        #endregion

//        #region Get All Tmp Details
//        [HttpGet]
//        public async Task<IActionResult> LoadTmpTable(string jobNo)
//        {
//            try
//            {
//                int? currentUser = await GetCurrentEmployeeIdAsync();
//                var data = await _service.GetDetailsWithCashBank(jobNo, currentUser);

//                if (data == null) data = new List<OFRAdjustApprovalDetailsVM>();

//                return Ok(data);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
//            }
//        }

//        #endregion

//        #region Adjust Approve Amount
//        [HttpPost]
//        public async Task<IActionResult> SaveAdjustApproval([FromBody] List<OFRAdjustApprovalDetailsSaveVM> model)
//        {
//            int? Approveduser = await GetCurrentEmployeeIdAsync();
//            if (model == null || !model.Any())
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "No approval data received"
//                });
//            }

//            var result = await _service.SaveAdjustApprovalAsync(model, Approveduser);

//            if (!result)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "Approve Failed"
//                });
//            }

//            return Ok(new
//            {
//                success = true,
//                message = "Approved  Successfully"
//            });
//        }
//        #endregion

//        #region When Click Edit Button Then Load Data Input field

//        [HttpGet]
//        public async Task<IActionResult> EditDetails(string jobno, BaseViewModel model)
//        {
//            try
//            {
//                int? currentUserID = await GetCurrentEmployeeIdAsync();

//                // Get master data
//                var masterData = await _service.GetRequisitionMasterByJobNo(jobno);

//                if (masterData == null)
//                {
//                    return Json(new { success = false, message = "No requisition found for this job no" });
//                }

//                // Copy details to temp table
//                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, currentUserID, model);

//                if (!detailsCopied)
//                {
//                    return Json(new
//                    {
//                        success = false,
//                        message = "Master data found but no details available",
//                        masterData = masterData
//                    });
//                }

//                return Json(new
//                {
//                    success = true,
//                    masterData = masterData,
//                    message = "Data loaded successfully"
//                });
//            }
//            catch (Exception ex)
//            {
//                return Json(new { success = false, message = ex.Message });
//            }
//        }
//        #endregion
//    }
//}
