using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.Interfaces.Services;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.HrmEmployees2;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.HrmEmployees2;
using GCTL.UI.Core.ViewModels.HrmEmployees2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace GCTL.UI.Core.Controllers
{
    public class HrmEmployee2Controller : BaseController
    {
        #region Service & Repository
        private readonly IHrmEmployee2Service empService;
        private readonly ICommonService commonService;
        private readonly IRepository<HrmDefBloodGroup> bloodRepository;
        private readonly IRepository<HrmDefSex> sexRepository;
        private readonly IRepository<HrmDefReligion> religionRepository;
        private readonly IRepository<HrmDefMaritalStatus> maritalRepository;
        private readonly IRepository<CoreBranch> coreBranchRepository;
        private readonly IRepository<CoreCompany> coreCompanyRepository;
        private readonly IRepository<HrmDefNationality> nationalityRepositopry;
        private readonly IHrmEmployeeBulkImportService _bulkImportService;
        string strMaxNO = string.Empty;

        public HrmEmployee2Controller(IHrmEmployee2Service empService, ICommonService commonService, IRepository<HrmDefBloodGroup> bloodRepository, IRepository<HrmDefSex> sexRepository, IRepository<HrmDefReligion> religionRepository, IRepository<HrmDefMaritalStatus> maritalRepository, IRepository<CoreBranch> coreBranchRepository, IRepository<CoreCompany> coreCompanyRepository, IRepository<HrmDefNationality> nationalityRepositopry, IHrmEmployeeBulkImportService bulkImportService)
        {
            this.empService = empService;
            this.commonService = commonService;
            this.bloodRepository = bloodRepository;
            this.sexRepository = sexRepository;
            this.religionRepository = religionRepository;
            this.maritalRepository = maritalRepository;
            this.coreBranchRepository = coreBranchRepository;
            this.coreCompanyRepository = coreCompanyRepository;
            this.nationalityRepositopry = nationalityRepositopry;
            _bulkImportService = bulkImportService;
        }



        #endregion

        #region Index

        public async Task<IActionResult> Index(string id)
        {
            var hasPermission = await empService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }
            HrmEmployee2PageViewModel model = new HrmEmployee2PageViewModel();
            //var list = await empService.GetAllAsync();
            //model.ListTableData = list ?? new List<HrmEmployee2SetUpViewModel>();
            if (!string.IsNullOrEmpty(id))
            {
                model.Setup = await empService.GetByIdAsync(id);
            }

            ViewBag.SexesDD = new SelectList(sexRepository.All(), "SexCode", "Sex");
            ViewBag.BloodGroupDD = new SelectList(bloodRepository.All(), "BloodGroupCode", "BloodGroup");
            ViewBag.ReligionDD = new SelectList(religionRepository.All(), "ReligionCode", "Religion");
            ViewBag.NationalityDD = new SelectList(nationalityRepositopry.All(), "NationalityCode", "Nationality");
            ViewBag.CoreBranchDD = new SelectList(coreBranchRepository.All(), "BranchCode", "BranchName");
            ViewBag.CoreCompanyDD = new SelectList(coreCompanyRepository.All(), "CompanyCode", "CompanyName");
            ViewBag.MaritalyDD = new SelectList(maritalRepository.All(), "MaritalStatusCode", "MaritalStatus");
            model.AddUrl = Url.Action(nameof(Setup));
            model.PageUrl = Url.Action(nameof(Index));
            //model.ShowHeaderButton = true;
            //model.HeaderButtonText = "Add Employee";
            //model.HeaderButtonUrl = Url.Action("Setup", "HrmEmployee2");
            //model.HeaderButtonIcon = "fa fa-plus";
            return View(model);

        }

        #endregion

        #region GetBranchesByCompanyCode


        [HttpGet]
        public async Task<JsonResult> GetBranchesByCompanyCode(string companyCode)
        {

            var branches = await (from c in coreCompanyRepository.All()
                                  join br in coreBranchRepository.All() on c.CompanyCode equals br.CompanyCode into cBraJoin
                                  from br in cBraJoin.DefaultIfEmpty()
                                  where c.CompanyCode == companyCode
                                  select new
                                  {
                                      value = br.BranchCode,
                                      text = br.BranchName ?? "No Branch Available"
                                  }).ToListAsync();



            return Json(branches);



        }

        #endregion

        #region Setup

        public async Task<ActionResult> Setup(string id)
        {

            var model = await empService.GetByIdAsync(id)
 ?? new HrmEmployee2SetUpViewModel();


            var companies = coreCompanyRepository.All();

            if (companies.Count() == 1)
            {
                model.CompanyCode = companies.First().CompanyCode;
            }
            else if (id != null)
            {
                model.CompanyCode = (await empService.GetByIdAsync(id))?.CompanyCode;
            }
            else
            {
                model.CompanyCode = null;
            }


            ViewBag.CoreCompanyDD = new SelectList(companies, "CompanyCode", "CompanyName", model.CompanyCode);
            ViewBag.SexesDD = new SelectList(sexRepository.All(), "SexCode", "Sex", model.SexCode);
            ViewBag.BloodGroupDD = new SelectList(bloodRepository.All(), "BloodGroupCode", "BloodGroup", model.BloodGroupCode);
            ViewBag.ReligionDD = new SelectList(religionRepository.All(), "ReligionCode", "Religion", model.ReligionCode);
            ViewBag.NationalityDD = new SelectList(nationalityRepositopry.All(), "NationalityCode", "Nationality", model.NationalityCode);
            ViewBag.CoreBranchDD = new SelectList(coreBranchRepository.All(), "BranchCode", "BranchName", model.BranchCode);
            ViewBag.MaritalyDD = new SelectList(maritalRepository.All(), "MaritalStatusCode", "MaritalStatus", model.MaritalStatusCode);
            model.AddUrl = Url.Action(nameof(Setup));

            //if (string.IsNullOrEmpty(model.EmployeeId))
            //{
            //    model.EmployeeId = await empService.GenerateNextCode();
            //   // commonService.FindMaxNo(ref strMaxNO, "EmployeeID", "HRM_Employee", 12);
            //}


            return View(model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> Setup(HrmEmployee2SetUpViewModel modelVM)
        {
            try
            {

                //if (!ModelState.IsValid)
                //{
                //    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                //    return Json(new { isSuccess = false, message = "Validation failed", errors });
                //}


                if (await empService.IsExistByAsync(modelVM.EmployeeId, modelVM.FirstName, modelVM.DateOfBirthOrginal?.ToString("MM/dd/yyyy"), modelVM.FatherName))
                {
                    return Json(new { isSuccess = false, message = $"Already  Exists!", isDuplicate = true });
                }


                //if (string.IsNullOrEmpty(modelVM.EmployeeId))
                //{
                //    modelVM.EmployeeId = await empService.GenerateNextCode();
                //    //commonService.FindMaxNo(ref strMaxNO, "EmployeeID", "HRM_Employee", 12);
                //}


                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0)
                {

                    var hasSavePermission = await empService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await empService.SaveAsync(modelVM, LoginInfo.CompanyCode);
                        //TempData["Data"] = "This is the data I want to share.";
                        //return RedirectToAction("Index", "HrmEmployee2");

                        return Json(new { isSuccess = true, message = "Saved Successfully", redirectUrl = Url.Action("Index", "HrmEmployee2"), lastCode = modelVM.EmployeeId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await empService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await empService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully", redirectUrl = Url.Action("Index", "HrmEmployee2"), lastCode = modelVM.EmployeeId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update", noUpdatePermission = true });
                    }
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {
                var hasPermission = await empService.DeletePermissionAsync(LoginInfo.AccessCode);

                if (hasPermission)
                {
                    DeleteHistoryViewModel model = new DeleteHistoryViewModel();
                    model.ToAudit(LoginInfo);
                    model.CompanyCode = LoginInfo.CompanyCode;

                    var result = await empService.DeleteLeaveType(ids, model);

                    // ✅ Fix 3: Use consistent property name refError
                    return Json(new
                    {
                        isSuccess = result.isSuccess,
                        message = result.message,
                        refError = result.refError
                    });
                }
                else
                {
                    return Json(new { isSuccess = false, message = "You have no access", refError = false });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting: {ex.Message}");
                return StatusCode(500, new { isSuccess = false, message = ex.Message, refError = false });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> Delete([FromBody] List<string> ids)
        //{
        //    try
        //    {
        //        var hasPermission = await empService.DeletePermissionAsync(LoginInfo.AccessCode);
        //        if (hasPermission)
        //        {
        //            DeleteHistoryViewModel model = new DeleteHistoryViewModel();

        //            model.ToAudit(LoginInfo);
        //            model.CompanyCode = LoginInfo.CompanyCode;  
        //            var result = await empService.DeleteLeaveType(ids, model);
        //            return Json(new { isSuccess = result.isSuccess, message = result.message, refSuccess = result.refError });
        //        }
        //        else
        //        {
        //            return Json(new { isSuccess = false, message = "You have no access" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error deleting: {ex.Message}");
        //        return StatusCode(500, new { isSuccess = false, message = ex.Message });
        //    }
        //}

        #endregion

        #region NeaxtCode

        [HttpGet]

        //public async Task<IActionResult> GenerateNextCode()
        //{
        //   // var nextCode = await empService.GenerateNextCode();
        //    return Json(nextCode);

        //}
        #endregion

        #region TabeleLodaing

    


        [HttpPost]                          // DataTables SS uses POST by default
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                // ── 1. Read DataTables request params from form body ──────────
                var draw = int.TryParse(Request.Form["draw"], out var d) ? d : 0;
                var start = int.TryParse(Request.Form["start"], out var s) ? s : 0;
                var length = int.TryParse(Request.Form["length"], out var l) ? l : 10;
                var searchValue = Request.Form["search[value]"].ToString().Trim();
                var orderColIdx = int.TryParse(Request.Form["order[0][column]"], out var oc) ? oc : 0;
                var orderDir = Request.Form["order[0][dir]"].ToString();

                // ── 2. Map column index → column name (must match JS columns:) ─
                var columnNames = new[]
                {
                    "AutoId",       // 0  (checkbox col – fallback)
                    "EmployeeId",   // 1
                    "FirstName",    // 2
                    "FirstName",    // 3  bangla – sort by same field
                    "SexCode",      // 4
                    "NationalityCode", // 5
                    "NationalIdno", // 6
                    "Telephone",    // 7
                    "AutoId",       // 8  photo
                    "AutoId"        // 9  actions
                };

                var sortColumn = (orderColIdx >= 0 && orderColIdx < columnNames.Length)
                    ? columnNames[orderColIdx]
                    : "AutoId";

                var sortDirection = (orderDir == "asc") ? "ASC" : "DESC";

                int pageNumber = (length > 0) ? (start / length) + 1 : 1;
                int pageSize = (length > 0) ? length : 10;

                // ── 3. Call service (stored procedure) ───────────────────────
                var result = await empService.GetPagedAsync(
                    pageNumber, pageSize, sortColumn, sortDirection, searchValue);

                // ── 4. Return DataTables JSON format ─────────────────────────
                return Json(new DataTableResponseDto<HrmEmployee2SetUpViewModel>
                {
                    Draw = draw,
                    RecordsTotal = result.TotalRecords,   // total without filter
                    RecordsFiltered = result.FilteredRecords,   // SP already filters
                    Data = result.Items
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        private void Delete_Photo(string id)
        {
            string sql = "Delete from HRM_EmployeePhoto where EmployeeID='" + id + "'";
        }

        #endregion

        #region CheckAvailability

        [HttpPost] //
        public async Task<JsonResult> CheckAvailability(string code, string firstName, string dateOfBirthOriginal, string fathersName)
        {

            if (await empService.IsExistByAsync(code, firstName, dateOfBirthOriginal, fathersName))
            {
                return Json(new { isSuccess = true, message = $"Already exists!." });
            }

            return Json(new { isSuccess = false });
        }


        #endregion

        #region DownloadExcel

        [HttpGet]
        public IActionResult DownloadExcel()
        {
            var fileBytes = _bulkImportService.GenerateExcelTemplate();
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EmployeeTableFormat.xlsx"
            );
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            try
            {
                if (excelFile == null || excelFile.Length == 0)
                    return Json(new { isSuccess = false, message = "Please select a file to upload." });

                var ext = Path.GetExtension(excelFile.FileName).ToLower();
                if (ext != ".xlsx" && ext != ".xls")
                    return Json(new { isSuccess = false, message = "Only .xlsx or .xls files are allowed.", isFormatError = true });

                var result = await _bulkImportService.BulkImportFromExcelAsync(
                    excelFile,
                    companyCode: LoginInfo.CompanyCode,
                    luser: LoginInfo.EmployeeId ?? string.Empty,
                    lip: GetLocalIP() ?? string.Empty,
                    lmac: GetMacAddress() ?? string.Empty
                );

                return Json(new
                {
                    isSuccess = result.IsSuccess,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    failCount = result.FailCount,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }

        public string GetLocalIP()
        {
            string ipAddress = string.Empty;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var networkInterface in networkInterfaces)
            {
                var properties = networkInterface.GetIPProperties();
                var ipv4Address = properties.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                if (ipv4Address != null)
                {
                    ipAddress = ipv4Address.Address.ToString();
                    break;
                }
            }

            return ipAddress;
        }

        public string GetMacAddress()
        {
            string macAddress = string.Empty;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
           .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            foreach (var networkInterface in networkInterfaces)
            {
                macAddress = networkInterface.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(macAddress))
                {
                    break;
                }
            }

            return macAddress;
        }

        #endregion

    }
}
