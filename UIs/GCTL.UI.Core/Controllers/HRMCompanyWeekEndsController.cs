using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.Departments;
using GCTL.Core.ViewModels.HRMCompanyWeekEnds;
using GCTL.Service.Common;
using GCTL.Service.Departments;
using GCTL.Service.HRMCompanyWeekEnds;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.HRMCompanyWeekEnds;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GCTL.UI.Core.Controllers
{
    public class HRMCompanyWeekEndsController : BaseController
    {

        private readonly IHRMCompanyWeekEndService hrmAtdComapanyWeekendService;
        private readonly ICommonService commonService;
        private readonly ICompositeViewEngine viewEngine;
        public HRMCompanyWeekEndsController(IHRMCompanyWeekEndService hrmAtdComapanyWeekendService, ICommonService commonService, ICompositeViewEngine viewEngine)
        {
            this.hrmAtdComapanyWeekendService = hrmAtdComapanyWeekendService;
            this.commonService = commonService;
            this.viewEngine = viewEngine;

        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null, string? id = null)
        {
            var hasPermission = await hrmAtdComapanyWeekendService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new HRMCompanyWeekEndPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await hrmAtdComapanyWeekendService.GetAllAsync() ?? new List<HRMCompanyWeekEndSetupViewModel>();
                model.HRMCompanyWeekEndList = list;

                //if (!string.IsNullOrWhiteSpace(searchTerm))
                //{
                //    list = list.Where(x => (x.CompanyWeekEndCode != null && x.CompanyWeekEndCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                //                        || (x.Weekend != null && string.Join(",", x.Weekend).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                //               .ToList();
                //}

                //int totalRecords = list.Count;
                //var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                //model.DynamicGrid = GridBuilder.BuildGrid(
                //    data: pagedList,
                //    totalRecords: totalRecords,
                //    currentPage: page,
                //    pageSize: pageSize,
                //    headers: new List<string> { "Code", "Weekend Day(s)", "Effective Date" },
                //    rowMapper: item => new Dictionary<string, object?>
                //    {
                //{ "CompanyWeekEndCode", item.CompanyWeekEndCode },
                //{ "Weekend Day(s)", string.Join(", ", item.Weekend) },
                //{ "Effective Date", item.EffectiveDate.ToString("dd/MM/yyyy") }
                //    },
                //    ajaxUrl: Url.Action("GetTableData", "HRMCompanyWeekEnds") ?? "/HRMCompanyWeekEnds/GetTableData",
                //    primaryKey: "CompanyWeekEndCode",
                //    hasCheckbox: true,
                //    actionUrlPattern: "/HRMCompanyWeekEnds/Setup/{id}",
                //    gridId: "companyWeekEndsGridContainer",
                //    editClass: "js-companyWeekEnds-edit",
                //    columnAlignments: new Dictionary<string, string>
                //    {
                //{ "CompanyWeekEndCode", "center" },
                //{ "Weekend Day(s)", "center" },
                //{ "Effective Date", "center" }
                //    },
                //    searchTerm: searchTerm,
                //    columnWidths: new Dictionary<string, string>
                //    {
                //{ "SelectAll", "120px" },
                //{ "Code", "100px" },
                //{ "Weekend Day(s)", "auto" },
                //{ "Effective Date", "150px" }
                //    }
                //);

                if (!string.IsNullOrEmpty(id))
                {
                    model.Setup = await hrmAtdComapanyWeekendService.GetByIdAsync(id) ?? new HRMCompanyWeekEndSetupViewModel();
                }
                else
                {
                    model.Setup = new HRMCompanyWeekEndSetupViewModel
                    {
                        CompanyWeekEndCode = await hrmAtdComapanyWeekendService.GenerateNextCode()
                    };
                }
            }
            catch (Exception ex)
            {
                model.HRMCompanyWeekEndList = new List<HRMCompanyWeekEndSetupViewModel>();
                model.Setup = new HRMCompanyWeekEndSetupViewModel();
                Console.WriteLine("Error: " + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            var model = new HRMCompanyWeekEndSetupViewModel();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await hrmAtdComapanyWeekendService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.CompanyWeekEndCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
                }
            }
            else
            {
                model.CompanyWeekEndCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new HRMCompanyWeekEndPageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await hrmAtdComapanyWeekendService.GetAllAsync() ?? new List<HRMCompanyWeekEndSetupViewModel>();
            //int totalRecords = list.Count;
            //var pagedList = list.Take(10).ToList();

            //pageModel.DynamicGrid = GridBuilder.BuildGrid(
            //    data: pagedList,
            //    totalRecords: totalRecords,
            //    currentPage: 1,
            //    pageSize: 10,
            //    headers: new List<string> { "Code", "Weekend Day(s)", "Effective Date" },
            //    rowMapper: item => new Dictionary<string, object?>
            //    {
            //{ "CompanyWeekEndCode", item.CompanyWeekEndCode },
            //{ "Weekend Day(s)", string.Join(", ", item.Weekend) },
            //{ "Effective Date", item.EffectiveDate.ToString("dd/MM/yyyy") }
            //    },
            //    ajaxUrl: Url.Action("GetTableData", "HRMCompanyWeekEnds") ?? "/HRMCompanyWeekEnds/GetTableData",
            //    primaryKey: "CompanyWeekEndCode",
            //    hasCheckbox: true,
            //    actionUrlPattern: "/HRMCompanyWeekEnds/Setup/{id}",
            //    gridId: "companyWeekEndsGridContainer",
            //    editClass: "js-companyWeekEnds-edit",
            //    columnAlignments: new Dictionary<string, string>
            //    {
            //{ "CompanyWeekEndCode", "center" },
            //{ "Weekend Day(s)", "center" },
            //{ "Effective Date", "center" }
            //    },
            //    columnWidths: new Dictionary<string, string>
            //    {
            //{ "SelectAll", "120px" },
            //{ "Code", "100px" },
            //{ "Weekend Day(s)", "auto" },
            //{ "Effective Date", "150px" }
            //    }
            //);

            return View("Index", pageModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(HRMCompanyWeekEndSetupViewModel modelVM)
        {
            try
            {
                var sortedWeekends = string.Join(",", modelVM.Weekend.OrderBy(w => w));
                var weekendCount = modelVM.Weekend.Count;

                if (weekendCount == 1)
                {
                    var singleWeekend = modelVM.Weekend.First();
                    if (await hrmAtdComapanyWeekendService.IsExistAsync(singleWeekend, modelVM.CompanyWeekEndCode, modelVM.EffectiveDate))
                    {
                        return Json(new { isSuccess = false, message = "Already exists!", isDuplicate = true });
                    }
                }
                else if (weekendCount > 1)
                {
                    if (await hrmAtdComapanyWeekendService.IsExistAsync(sortedWeekends, modelVM.CompanyWeekEndCode, modelVM.EffectiveDate))
                    {
                        return Json(new { isSuccess = false, message = $"The combination of <span style='color: blue;'>'{sortedWeekends}'</span> already exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                if (string.IsNullOrEmpty(modelVM.CompanyWeekEndCode))
                {
                    modelVM.CompanyWeekEndCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);

                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    var hasSavePermission = await hrmAtdComapanyWeekendService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
                    }

                    bool result = await hrmAtdComapanyWeekendService.SaveAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save." });
                }
                else
                {
                    var hasUpdatePermission = await hrmAtdComapanyWeekendService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update", noUpdatePermission = true });
                    }

                    bool result = await hrmAtdComapanyWeekendService.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
                        return Json(new { isSuccess = true, message = "Updated Successfully", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { isSuccess = false, message = "An error occurred while saving." });
            }
        }

        #endregion

        //#region GetByallId
        //public async Task<IActionResult> Index(string? id)
        //{
        //    var hasPermission = await hrmAtdComapanyWeekendService.PagePermissionAsync(LoginInfo.AccessCode);
        //    if (!hasPermission)
        //    {
        //        return RedirectToAction("Login", "Accounts");
        //    }
        //    //Get all
        //    HRMCompanyWeekEndPageViewModel model = new HRMCompanyWeekEndPageViewModel();
        //    var list = await hrmAtdComapanyWeekendService.GetAllAsync();
        //    model.HRMCompanyWeekEndList = list ?? new List<HRMCompanyWeekEndSetupViewModel>();
        //    //Get By Id
        //    if (!string.IsNullOrEmpty(id))
        //    {

        //        model.Setup = await hrmAtdComapanyWeekendService.GetByIdAsync(id);

        //        //Get all but Ldate and ModifyDate


        //    }

        //    //

        //    //

        //    model.PageUrl = Url.Action(nameof(Index));
        //    return View(model);
        //}
        //#endregion


        //#region Post Update 

        //[HttpPost]
        //[ValidateAntiForgeryToken]


        //public async Task<IActionResult> Setup(HRMCompanyWeekEndSetupViewModel modelVM)
        //{
        //    try
        //    {

        //        //var sortedWeekends = string.Join(",", modelVM.Weekend.OrderBy(w => w));


        //        //if (await hrmAtdComapanyWeekendService.IsExistAsync(sortedWeekends, modelVM.CompanyWeekEndCode))
        //        //{
        //        //    return Json(new { isSuccess = false, message = $"The combination <span style='color: blue;'>'{sortedWeekends}'</span> already exists!", isDuplicate = true });
        //        //}


        //        var sortedWeekends = string.Join(",", modelVM.Weekend.OrderBy(w => w));
        //        var weekendCount = modelVM.Weekend.Count;

        //        if (weekendCount == 1)
        //        {

        //            var singleWeekend = modelVM.Weekend.First();
        //            if (await hrmAtdComapanyWeekendService.IsExistAsync(singleWeekend, modelVM.CompanyWeekEndCode,modelVM.EffectiveDate))
        //            {

        //                return Json(new { isSuccess = false, message = $" Already exists!", isDuplicate = true });
        //            }
        //        }
        //        else if (weekendCount > 1)
        //        {

        //            if (await hrmAtdComapanyWeekendService.IsExistAsync(sortedWeekends, modelVM.CompanyWeekEndCode,modelVM.EffectiveDate))
        //            {

        //                return Json(new { isSuccess = false, message = $"The combination of <span style='color: blue;'>'{sortedWeekends}'</span> already exists!", isDuplicate = true });
        //            }
        //        }



        //        if (string.IsNullOrEmpty(modelVM.CompanyWeekEndCode))
        //        {
        //            modelVM.CompanyWeekEndCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
        //        }

        //        modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
        //        if (modelVM.AutoId == 0 || modelVM.AutoId == null)
        //        {
        //            var hasSavePermission = await hrmAtdComapanyWeekendService.SavePermissionAsync(LoginInfo.AccessCode);
        //            if (hasSavePermission)
        //            {
        //                await hrmAtdComapanyWeekendService.SaveAsync(modelVM);
        //                return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = modelVM.CompanyWeekEndCode });
        //            }
        //            else
        //            {
        //                return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
        //            }
        //        }
        //        else
        //        {

        //            var hasUpdatePermission = await hrmAtdComapanyWeekendService.UpdatePermissionAsync(LoginInfo.AccessCode);
        //            if (hasUpdatePermission)
        //            {
        //                await hrmAtdComapanyWeekendService.UpdateAsync(modelVM);
        //                return Json(new { isSuccess = true, message = "Updated Successfully", lastCode = modelVM.CompanyWeekEndCode });
        //            }
        //            else
        //            {
        //                return Json(new { isSuccess = false, message = "You have no access to update", noUpdatePermission = true });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message);
        //    }
        //}




        //#endregion



        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {

                var hasPermission = await hrmAtdComapanyWeekendService.DeletePermissionAsync(LoginInfo.AccessCode);
                if (hasPermission)
                {

                    foreach (var id in ids)
                    {
                        var result = hrmAtdComapanyWeekendService.DeleteLeaveType(id);

                    }

                    return Json(new { isSuccess = true, message = "Deleted Successfully" });
                }
                else
                {

                    return Json(new { isSuccess = false, message = "You have no access" });
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error deleting  type: {ex.Message}");

                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }


        #endregion

        #region NeaxtCode
        [HttpGet]
        public async Task<IActionResult> GenerateNextCode()
        {
            var nextCode = await hrmAtdComapanyWeekendService.GenerateNextCode();
            return Json(nextCode);
        }
        #endregion

        #region CheckAvailability

        [HttpPost]


        public async Task<JsonResult> CheckAvailability(List<string> weekends, string code ,DateTime effectiveDate)
        {
          
            var sortedWeekends = string.Join(",", weekends.OrderBy(w => w));

           
            bool exists = await hrmAtdComapanyWeekendService.IsExistAsync(sortedWeekends, code, effectiveDate);

            if (exists)
            {
                return Json(new { isSuccess = true, message = $"Already exists!" });
                // return Json(new { isSuccess = true, message = $"This data <span style='color: blue;'>'{sortedWeekends}'</span> already exists!" });
            }

            return Json(new { isSuccess = false });
        }







        #endregion

        #region TabeleLodaing

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await hrmAtdComapanyWeekendService.GetAllAsync() ?? new List<HRMCompanyWeekEndSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<HRMCompanyWeekEndSetupViewModel>(), error = ex.Message });
            }
        }
        #endregion

    }
    


    
}




