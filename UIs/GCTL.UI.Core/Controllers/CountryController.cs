using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.Country;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Service.Common;
using GCTL.Service.Country;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Country;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class CountryController : BaseController
    {
        #region Service & Repository
        public readonly ICountryService countryService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public CountryController(
            ICountryService countryService,
            ICommonService commonService
        )
        {
            this.countryService = countryService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await countryService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new CountryPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await countryService.GetAllAsync() ?? new List<CountrySetuoViewModel>();
                model.CountryList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.CountryId != null && x.CountryId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.CountryName != null && x.CountryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Country Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CountryId", item.CountryId },
                        { "Country Name", item.CountryName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Country") ?? "/Country/GetTableData",
                    primaryKey: "CountryId",
                    hasCheckbox: true,
                    actionUrlPattern: "/Country/Setup/{id}",
                    gridId: "countryGridContainer",
                    editClass: "js-country-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CountryId", "center" },
                        { "Country Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Country Name", "auto" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "CountryId", "CA_Def_Country", 3);
                model.Setup = new CountrySetuoViewModel
                {
                    CountryId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.CountryList = new List<CountrySetuoViewModel>();
                model.Setup = new CountrySetuoViewModel();
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
            CountrySetuoViewModel model = new CountrySetuoViewModel();
            commonService.FindMaxNo(ref strMaxNO, "CountryId", "CA_Def_Country", 3);

            if (!string.IsNullOrEmpty(id))
            {
                model = await countryService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.CountryId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await countryService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new CountryPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await countryService.GetAllAsync() ?? new List<CountrySetuoViewModel>();
                indexModel.CountryList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Country Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CountryId", item.CountryId },
                        { "Country Name", item.CountryName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Country") ?? "/Country/GetTableData",
                    primaryKey: "CountryId",
                    hasCheckbox: true,
                    actionUrlPattern: "/Country/Setup/{id}",
                    gridId: "countryGridContainer",
                    editClass: "js-country-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CountryId", "center" },
                        { "Country Name", "left" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Country Name", "auto" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.CountryList = new List<CountrySetuoViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CountrySetuoViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.Tc == null)
                {
                    if (await countryService.IsExistAsync(modelVM.CountryName, modelVM.CountryId))
                    {
                        return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.Tc > 0);
                if (modelVM.Tc == 0 || modelVM.Tc == null)
                {
                    var hasSavePermission = await countryService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await countryService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.CountryId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await countryService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await countryService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.CountryId });
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
            if (await countryService.IsExistAsync(name, code))
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

            var hasPermission = await countryService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await countryService.DeleteTab(ids, model);
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
                var list = await countryService.GetAllAsync() ?? new List<CountrySetuoViewModel>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.CountryId != null && x.CountryId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.CountryName != null && x.CountryName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var gridModel = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Country Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CountryId", item.CountryId },
                        { "Country Name", item.CountryName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Country") ?? "/Country/GetTableData",
                    primaryKey: "CountryId",
                    hasCheckbox: true,
                    actionUrlPattern: "/Country/Setup/{id}",
                    gridId: "countryGridContainer",
                    editClass: "js-country-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CountryId", "center" },
                        { "Country Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Country Name", "auto" }
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
