//using GCTL.Core.ViewModels;
//using GCTL.Core.ViewModels.OFRSection.OFRApproval;
//using GCTL.Service.Language;
//using GCTL.Service.OFRApproval;
//using GCTL.Service.UserProfile;
//using Microsoft.AspNetCore.Mvc;

//namespace GCTL_App.Controllers.OFRApproval
//{
//    public class OFRApprovalController : BaseController
//    {
//        #region Service
//        private readonly IOFRApproval _service;

//        public OFRApprovalController(ITranslateService translateService, IUserProfileService userProfileService, IOFRApproval service) : base(translateService, userProfileService)
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

//        #region Get All Requisition List for Top Grid
//        public async Task<IActionResult> GetAllRequisitionList(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "")
//        {
//            try
//            {
//                var result = await _service.GetAllRequisition(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, customerid, shipmentmodeid);

//                if (result.Data == null || !result.Data.Any())
//                {
//                    return Ok(new
//                    {
//                        Data = new List<OFRApprovalTopGridVM>(),
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

//        #region Get All Requisition List for Bottom Grid
//        public async Task<IActionResult> GetAllRequisitionForTopGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc")
//        {
//            try
//            {
//                var result = await _service.AllRequisitionforBottomGrid(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

//                if (result.Data == null || !result.Data.Any())
//                {
//                    return Ok(new
//                    {
//                        Data = new List<OFRApprovalBottomGridVM>(),
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
//        //11241
//        //11267

//        #region When Click Radion Button Then Load Data Input field
//        //[HttpGet]
//        //public async Task<IActionResult> GetrequisitionData(string jobno)
//        //{
//        //    int? currentUserID = await GetCurrentEmployeeIdAsync();

//        //    var data = await _service.GetRequisitionMasterByJobNo(jobno);
//        //    return Json(data);
//        //}
//        [HttpGet]
//        public async Task<IActionResult> GetRequisitionDataWithDetails(string jobno)
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
//                bool detailsCopied = await _service.CopyDetailsToTmp(jobno, currentUserID);

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
//        public async Task<IActionResult> GetAllTmpDetails(string jobNo)
//        {
//            try
//            {
//                int? currentUser = await GetCurrentEmployeeIdAsync();
//                var data = await _service.GetAllTmpAsync(jobNo, currentUser);

//                if (data == null) data = new List<OFRApprovalDetailsVM>();

//                return Ok(data);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
//            }
//        }

//        #endregion

//        #region Bank Account Dropdown
//        public async Task<IActionResult> BankAccountDD(string value)
//        {
//            var data = await _service.GetBankAccount(value);
//            return Json(data);
//        }
//        #endregion

//        #region Approve Estimate Amount
//        [HttpPost]
//        public async Task<IActionResult> SaveApproval([FromBody] List<OFRApprovalDetailsSaveVM> model)
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

//            var result = await _service.SaveApprovalAsync(model, Approveduser);

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
//        public async Task<IActionResult> EditButtonClicked(string jobno, BaseViewModel model)
//        {
//            try
//            {
//                int? currentUserID = await GetCurrentEmployeeIdAsync();

//                // Get master data
//                var masterData = await _service.GetRequisitionInputData(jobno);

//                if (masterData == null)
//                {
//                    return Json(new { success = false, message = "No requisition found for this job no" });
//                }

//                // Copy details to temp table
//                bool detailsCopied = await _service.CopyDetailsToTmpTable(jobno, currentUserID, model);

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
