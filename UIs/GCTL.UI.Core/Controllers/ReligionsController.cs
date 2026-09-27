using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Religions;
using GCTL.Service.Common;
using GCTL.Service.Religions;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Religions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class ReligionsController : BaseController
    {
        #region Ser & Repo
        private readonly IReligionService religionService;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;
        public ReligionsController(
            ICommonService commonService,
            IReligionService religionService
        )
        {
            this.religionService = religionService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await religionService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ReligionPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await religionService.GetAllAsync() ?? new List<ReligionSetupViewModel>();
                model.ReligionList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.ReligionCode != null && x.ReligionCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.Religion != null && x.Religion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.ShortName != null && x.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Religion Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "ReligionCode", item.ReligionCode },
                        { "Religion Name", item.Religion },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Religions") ?? "/Religions/GetTableData",
                    primaryKey: "ReligionCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/Religions/Setup/{id}",
                    gridId: "religionGridContainer",
                    editClass: "js-religion-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "ReligionCode", "center" },
                        { "Religion Name", "left" },
                        { "Short Name", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Religion Name", "auto" },
                        { "Short Name", "140px" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "ReligionCode", "HRM_Def_Religion", 1);
                model.Setup = new ReligionSetupViewModel
                {
                    ReligionCode = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.ReligionList = new List<ReligionSetupViewModel>();
                model.Setup = new ReligionSetupViewModel();
                Console.WriteLine("Error" + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            ReligionSetupViewModel model = new ReligionSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "ReligionCode", "HRM_Def_Religion", 1);

            if (!string.IsNullOrEmpty(id))
            {
                model = await religionService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.ReligionCode = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await religionService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new ReligionPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await religionService.GetAllAsync() ?? new List<ReligionSetupViewModel>();
                indexModel.ReligionList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Religion Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "ReligionCode", item.ReligionCode },
                        { "Religion Name", item.Religion },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Religions") ?? "/Religions/GetTableData",
                    primaryKey: "ReligionCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/Religions/Setup/{id}",
                    gridId: "religionGridContainer",
                    editClass: "js-religion-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "ReligionCode", "center" },
                        { "Religion Name", "left" },
                        { "Short Name", "center" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Religion Name", "auto" },
                        { "Short Name", "140px" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.ReligionList = new List<ReligionSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(ReligionSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    if (await religionService.IsExistAsync(modelVM.Religion, modelVM.ReligionCode))
                    {
                        return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    var hasSavePermission = await religionService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await religionService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.ReligionCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await religionService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await religionService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.ReligionCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                return RedirectToAction("Login", "Accounts");
            }
        }

        #endregion

        #region CheckAvailability
        [HttpPost]
        public async Task<JsonResult> CheckAvailability(string name, string code)
        {
            if (await religionService.IsExistAsync(name, code))
            {
                return Json(new { isSuccess = true, message = $"Already Exists!" });
            }

            return Json(new { isSuccess = false });
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

            var hasPermission = await religionService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await religionService.DeleteTab(ids, model);
            if (success.succses)
            {
                return Json(new { success = true, message = "Deleted Successfully." });
            }
            else
            {
                return Json(new { success = false, message = success.messege });
            }
        }

        #endregion

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await religionService.GetAllAsync() ?? new List<ReligionSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<ReligionSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion
    }
}