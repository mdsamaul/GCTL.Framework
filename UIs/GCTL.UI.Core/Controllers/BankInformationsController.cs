using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.BankInformations;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Service.BankInformations;
using GCTL.Service.Common;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.BankInformations;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class BankInformationsController : BaseController
    {
        private readonly IBankInformationsService service;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public BankInformationsController(IBankInformationsService service, ICommonService commonService)
        {
            this.service = service;
            this.commonService = commonService;
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new BankInformationsPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await service.GetAllAsync() ?? new List<BankInformationsSetupViewModel>();
                model.tableDataList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.BankId != null && x.BankId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankName != null && x.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
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
                    headers: new List<string> { "Code", "Bank Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BankId", item.BankId },
                        { "Bank Name", item.BankName },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankInformations") ?? "/BankInformations/GetTableData",
                    primaryKey: "BankId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankInformations/Setup/{id}",
                    gridId: "bankInformationGridContainer",
                    editClass: "js-bankinformation-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BankId", "center" },
                        { "Bank Name", "left" },
                        { "Short Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Bank Name", "auto" },
                        { "Short Name", "150px" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "BankId", "SALES_Def_BankInfo", 2);
                model.Setup = new BankInformationsSetupViewModel
                {
                    BankId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.tableDataList = new List<BankInformationsSetupViewModel>();
                model.Setup = new BankInformationsSetupViewModel();
                Console.WriteLine("Error: " + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            BankInformationsSetupViewModel model = new BankInformationsSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "BankId", "SALES_Def_BankInfo", 2);

            if (!string.IsNullOrEmpty(id))
            {
                model = await service.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.BankId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new BankInformationsPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await service.GetAllAsync() ?? new List<BankInformationsSetupViewModel>();
                indexModel.tableDataList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Bank Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BankId", item.BankId },
                        { "Bank Name", item.BankName },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankInformations") ?? "/BankInformations/GetTableData",
                    primaryKey: "BankId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankInformations/Setup/{id}",
                    gridId: "bankInformationGridContainer",
                    editClass: "js-bankinformation-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BankId", "center" },
                        { "Bank Name", "left" },
                        { "Short Name", "left" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Bank Name", "auto" },
                        { "Short Name", "150px" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.tableDataList = new List<BankInformationsSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(BankInformationsSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    if (await service.IsExistAsync(modelVM.BankName, modelVM.BankId))
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
                    var hasSavePermission = await service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await service.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.BankId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await service.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.BankId });
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
            if (await service.IsExistAsync(name, code))
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

            var hasPermission = await service.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await service.DeleteTab(ids, model);
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
                var list = await service.GetAllAsync() ?? new List<BankInformationsSetupViewModel>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.BankId != null && x.BankId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankName != null && x.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.ShortName != null && x.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var gridModel = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Bank Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BankId", item.BankId },
                        { "Bank Name", item.BankName },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankInformations") ?? "/BankInformations/GetTableData",
                    primaryKey: "BankId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankInformations/Setup/{id}",
                    gridId: "bankInformationGridContainer",
                    editClass: "js-bankinformation-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BankId", "center" },
                        { "Bank Name", "left" },
                        { "Short Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Bank Name", "auto" },
                        { "Short Name", "150px" }
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