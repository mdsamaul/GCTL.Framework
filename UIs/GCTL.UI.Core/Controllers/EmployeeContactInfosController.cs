using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.EmployeeContactInfos;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.EmployeeContactInfos;
using GCTL.Service.HrmEmployees2;
using GCTL.UI.Core.ViewModels.EmployeeContactInfos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class EmployeeContactInfosController : BaseController
    {
        #region Service & Rep
        private readonly IEmployeeContactInfoService employeeContactInfoService;
        private readonly ICommonService commonService;
        private readonly IRepository<CoreBranch> coreBranchRepository;
        private readonly IRepository<CoreCompany> coreCompanyRepository;
        private readonly IRepository<HrmDefRelationship> relationshipRepository;
        private readonly IRepository<HrmDefDistrict> districtRepository;
        private readonly IHrmEmployee2Service hrmEmployee;

        string strMaxNO = "";

        public EmployeeContactInfosController( 
            ICommonService commonService,
            IEmployeeContactInfoService employeeContactInfoService,
            IRepository<CoreBranch> coreBranchRepository,
            IRepository<CoreCompany> coreCompanyRepository,
            IHrmEmployee2Service hrmEmployee,
            IRepository<HrmDefRelationship> relationshipRepository,
            IRepository<HrmDefDistrict> districtRepository

            )
        {
            this.commonService = commonService;
            this.employeeContactInfoService = employeeContactInfoService;
            this.coreBranchRepository = coreBranchRepository;
            this.coreCompanyRepository = coreCompanyRepository;
            this.hrmEmployee = hrmEmployee;
            this.relationshipRepository = relationshipRepository;
            this.districtRepository = districtRepository;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(string id)
        {
            var hasPermission = await employeeContactInfoService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }
            EmployeeContactInfosPageViewModel model = new EmployeeContactInfosPageViewModel();

            model.AddUrl = Url.Action(nameof(Setup));
            model.PageUrl = Url.Action(nameof(Index));
            model.ListTableData = await employeeContactInfoService.GetAllAsync();

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<ActionResult> Setup(string id)
        {
            var model = await employeeContactInfoService.GetByIdAsync(id) ?? new EmployeeContactInfosSetupViewModel();

            var companies = coreCompanyRepository.All();

            if (companies.Count() == 1)
            {
                model.CompanyCode = companies.First().CompanyCode; // Set the selected value in the model
            }
            else if (id != null) 
            {
                model.CompanyCode = (await employeeContactInfoService.GetByIdAsync(id))?.CompanyCode;
            }
            else
            {
                model.CompanyCode = null;
            }
            // Setup GET action 에
            ViewBag.SelectedBranchCode = model.BranchCode;
            ViewBag.SelectedBranchName = coreBranchRepository.All()
                .Where(x => x.BranchCode == model.BranchCode)
                .Select(x => x.BranchName)
                .FirstOrDefault() ?? "";
            ViewBag.RelationshipDD = new SelectList(relationshipRepository.All(), "RelationshipCode", "Relationship");
            ViewBag.CoreBranchDD = new SelectList(coreBranchRepository.All(), "BranchCode", "BranchName", model.BranchCode);
            ViewBag.CoreCompanyDD = new SelectList(coreCompanyRepository.All(), "CompanyCode", "CompanyName", model.CompanyCode);
            ViewBag.EmployeeDD = new SelectList(await hrmEmployee.GetEmployeeDropSelections(), "Code", "Name");
            ViewBag.DistrictDD = new SelectList(districtRepository.All(), "DistrictId", "District");

            model.AddUrl = Url.Action(nameof(Setup));
            if (model.EmpContactId == null)
            {
                model.EmpContactId = commonService.NextCode("EmpContactID", "HRM_EmployeeContactInfo", 4);
                ViewBag.IsEditMode = false;
            }
            else
            {
                ViewBag.IsEditMode = true;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            return View(model);
        }

        #endregion

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await employeeContactInfoService.GetAllAsync();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<EmployeeContactInfosSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region Post Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(EmployeeContactInfosSetupViewModel modelVM)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                    return Json(new { isSuccess = false, message = errors });
                }

                if (await employeeContactInfoService.IsExistByAsync(modelVM.EmpContactId, modelVM.EmployeeId))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }

                if (string.IsNullOrEmpty(modelVM.EmpContactId))
                {
                    modelVM.EmpContactId = await employeeContactInfoService.GenerateNextCode();
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0)
                {
                    var hasSavePermission = await employeeContactInfoService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        bool isSaved = await employeeContactInfoService.SaveAsync(modelVM, LoginInfo.CompanyCode);
                        if (isSaved)
                            return Json(new { isSuccess = true, message = "Saved Successfully", redirectUrl = Url.Action("Index", "EmployeeContactInfos"), lastCode = modelVM.EmpContactId });
                        else
                            return Json(new { isSuccess = false, message = "Failed to save data. Please check constraints." });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
                    }
                }
                else 
                {
                    var hasUpdatePermission = await employeeContactInfoService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        bool isUpdated = await employeeContactInfoService.UpdateAsync(modelVM);
                        if (isUpdated)
                            return Json(new { isSuccess = true, message = "Updated Successfully", redirectUrl = Url.Action("Index", "EmployeeContactInfos"), lastCode = modelVM.EmpContactId });
                        else
                            return Json(new { isSuccess = false, message = "Failed to update data." });
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
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { success = false, message = "No IDs provided for delete." });
            }

            var hasPermission = await employeeContactInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            bool success = await employeeContactInfoService.DeleteTab(ids, model);
            if (success)
            {
                return Json(new { success = true, message = "Deleted Successfully." });
            }
            else
            {
                return Json(new { success = false, message = "Deletion failed. Some entities may still exists." });
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> Delete([FromBody] List<string> ids)
        //{
        //    try
        //    {

        //        var hasPermission = await employeeContactInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
        //        if (hasPermission)
        //        {

        //            foreach (var id in ids)
        //            {
        //                var result = employeeContactInfoService.DeleteLeaveType(id);

        //            }

        //            return Json(new { isSuccess = true, message = "Deleted Successfully" });
        //        }
        //        else
        //        {

        //            return Json(new { isSuccess = false, message = "You have no access" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        Console.WriteLine($"Error deleting leave type: {ex.Message}");

        //        return StatusCode(500, new { isSuccess = false, message = ex.Message });
        //    }
        //}

        #endregion

        #region NeaxtCode

        [HttpGet]
        public async Task<IActionResult> GenerateNextCode()
        {
            var nextCode = await employeeContactInfoService.GenerateNextCode();
            return Json(nextCode);
        }

        #endregion



        #region CheckAvailability

        [HttpPost] 
        public async Task<JsonResult> CheckAvailability(string code, string EmployeeId)
        {

            if (await employeeContactInfoService.IsExistByAsync(code,  EmployeeId))
            {
                return Json(new { isSuccess = true, message = $"Already exists!." });
            }

            return Json(new { isSuccess = false });
        }

        #endregion

    }
}
