using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.BloodGroups;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Service.BloodGroups;
using GCTL.Service.Common;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.BloodGroups;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class BloodGroupsController : BaseController
    {
        #region Ser & Repo
        private readonly IBloodGroupService bloodGroupService;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;
        public BloodGroupsController(
            ICommonService commonService,
            IBloodGroupService bloodGroupService
        )
        {
            this.bloodGroupService = bloodGroupService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await bloodGroupService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new BloodGroupPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await bloodGroupService.GetAllAsync() ?? new List<BloodGroupSetupViewModel>();
                model.BloodGroupList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.BloodGroupCode != null && x.BloodGroupCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BloodGroup != null && x.BloodGroup.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BanglaBloodGroup != null && x.BanglaBloodGroup.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Blood Group", "Blood Group (বাংলা)" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BloodGroupCode", item.BloodGroupCode },
                        { "Blood Group", item.BloodGroup },
                        { "Blood Group (বাংলা)", item.BanglaBloodGroup }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BloodGroups") ?? "/BloodGroups/GetTableData",
                    primaryKey: "BloodGroupCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/BloodGroups/Setup/{id}",
                    gridId: "bloodGroupGridContainer",
                    editClass: "js-bloodgroup-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BloodGroupCode", "center" },
                        { "Blood Group", "left" },
                        { "Blood Group (বাংলা)", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Blood Group", "auto" },
                        { "Blood Group (বাংলা)", "180px" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "BloodGroupCode", "HRM_Def_BloodGroup", 2);
                model.Setup = new BloodGroupSetupViewModel
                {
                    BloodGroupCode = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.BloodGroupList = new List<BloodGroupSetupViewModel>();
                model.Setup = new BloodGroupSetupViewModel();
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
            BloodGroupSetupViewModel model = new BloodGroupSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "BloodGroupCode", "HRM_Def_BloodGroup", 2);

            if (!string.IsNullOrEmpty(id))
            {
                model = await bloodGroupService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.BloodGroupCode = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await bloodGroupService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new BloodGroupPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await bloodGroupService.GetAllAsync() ?? new List<BloodGroupSetupViewModel>();
                indexModel.BloodGroupList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Blood Group", "Blood Group (বাংলা)" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BloodGroupCode", item.BloodGroupCode },
                        { "Blood Group", item.BloodGroup },
                        { "Blood Group (বাংলা)", item.BanglaBloodGroup }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BloodGroups") ?? "/BloodGroups/GetTableData",
                    primaryKey: "BloodGroupCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/BloodGroups/Setup/{id}",
                    gridId: "bloodGroupGridContainer",
                    editClass: "js-bloodgroup-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BloodGroupCode", "center" },
                        { "Blood Group", "left" },
                        { "Blood Group (বাংলা)", "center" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Blood Group", "auto" },
                        { "Blood Group (বাংলা)", "180px" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.BloodGroupList = new List<BloodGroupSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(BloodGroupSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    if (await bloodGroupService.IsExistAsync(modelVM.BloodGroup, modelVM.BloodGroupCode))
                    {
                        return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, (modelVM.AutoId > 0 || modelVM.Tc > 0));
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    var hasSavePermission = await bloodGroupService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await bloodGroupService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.BloodGroupCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await bloodGroupService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await bloodGroupService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.BloodGroupCode });
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
            if (await bloodGroupService.IsExistAsync(name, code))
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

            var hasPermission = await bloodGroupService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await bloodGroupService.DeleteTab(ids, model);
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

        #region Table Loading (Dynamic Grid)

        [HttpGet]
        public async Task<IActionResult> GetTableData(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var list = await bloodGroupService.GetAllAsync() ?? new List<BloodGroupSetupViewModel>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.BloodGroupCode != null && x.BloodGroupCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BloodGroup != null && x.BloodGroup.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BanglaBloodGroup != null && x.BanglaBloodGroup.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var gridModel = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Blood Group", "Blood Group (বাংলা)" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BloodGroupCode", item.BloodGroupCode },
                        { "Blood Group", item.BloodGroup },
                        { "Blood Group (বাংলা)", item.BanglaBloodGroup }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BloodGroups") ?? "/BloodGroups/GetTableData",
                    primaryKey: "BloodGroupCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/BloodGroups/Setup/{id}",
                    gridId: "bloodGroupGridContainer",
                    editClass: "js-bloodgroup-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BloodGroupCode", "center" },
                        { "Blood Group", "left" },
                        { "Blood Group (বাংলা)", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Blood Group", "auto" },
                        { "Blood Group (বাংলা)", "180px" }
                    }
                );

                return PartialView("_DynamicGrid", gridModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion
    }
}