using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.CurrencyType;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Service.Common;
using GCTL.Service.CurrencyType;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.CurrencyType;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Controllers
{
    public class CurrencyTypeController : BaseController
    {
        private readonly ICurrencyType _service;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public CurrencyTypeController(ICurrencyType service, ICommonService commonService) : base()
        {
            _service = service;
            this.commonService = commonService;
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await _service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new CurrencyTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await _service.GetAllSetupAsync() ?? new List<CurrencyTypeSetupViewModel>();
                model.tableDataList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.CurrencyId != null && x.CurrencyId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.CurrencyName != null && x.CurrencyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.ShortName != null && x.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.Symbol != null && x.Symbol.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Currency Name", "Short Name", "Symbol", "Decimal Places", "Negative Format" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CurrencyId", item.CurrencyId },
                        { "Currency Name", item.CurrencyName },
                        { "Short Name", item.ShortName },
                        { "Symbol", item.Symbol },
                        { "Decimal Places", item.DecimalPlaces },
                        { "Negative Format", item.NegativeFormat }
                    },
                    ajaxUrl: Url.Action("GetTableData", "CurrencyType") ?? "/CurrencyType/GetTableData",
                    primaryKey: "CurrencyId",
                    hasCheckbox: true,
                    actionUrlPattern: "/CurrencyType/Setup/{id}",
                    gridId: "currencyTypeGridContainer",
                    editClass: "js-currencytype-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CurrencyId", "center" },
                        { "Currency Name", "left" },
                        { "Short Name", "left" },
                        { "Symbol", "center" },
                        { "Decimal Places", "center" },
                        { "Negative Format", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Currency Name", "auto" },
                        { "Short Name", "120px" },
                        { "Symbol", "100px" },
                        { "Decimal Places", "120px" },
                        { "Negative Format", "140px" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "CurrencyId", "CA_Def_Currency", 3);
                model.Setup = new CurrencyTypeSetupViewModel
                {
                    CurrencyId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.tableDataList = new List<CurrencyTypeSetupViewModel>();
                model.Setup = new CurrencyTypeSetupViewModel();
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
            CurrencyTypeSetupViewModel model = new CurrencyTypeSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "CurrencyId", "CA_Def_Currency", 3);

            if (!string.IsNullOrEmpty(id))
            {
                model = await _service.GetSetupByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.CurrencyId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // Direct browser URL navigation
            var hasPermission = await _service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new CurrencyTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await _service.GetAllSetupAsync() ?? new List<CurrencyTypeSetupViewModel>();
                indexModel.tableDataList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Currency Name", "Short Name", "Symbol", "Decimal Places", "Negative Format" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CurrencyId", item.CurrencyId },
                        { "Currency Name", item.CurrencyName },
                        { "Short Name", item.ShortName },
                        { "Symbol", item.Symbol },
                        { "Decimal Places", item.DecimalPlaces },
                        { "Negative Format", item.NegativeFormat }
                    },
                    ajaxUrl: Url.Action("GetTableData", "CurrencyType") ?? "/CurrencyType/GetTableData",
                    primaryKey: "CurrencyId",
                    hasCheckbox: true,
                    actionUrlPattern: "/CurrencyType/Setup/{id}",
                    gridId: "currencyTypeGridContainer",
                    editClass: "js-currencytype-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CurrencyId", "center" },
                        { "Currency Name", "left" },
                        { "Short Name", "left" },
                        { "Symbol", "center" },
                        { "Decimal Places", "center" },
                        { "Negative Format", "center" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Currency Name", "auto" },
                        { "Short Name", "120px" },
                        { "Symbol", "100px" },
                        { "Decimal Places", "120px" },
                        { "Negative Format", "140px" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.tableDataList = new List<CurrencyTypeSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CurrencyTypeSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 && modelVM.Tc == 0)
                {
                    if (await _service.IsExistByNameAsync(modelVM.CurrencyName, modelVM.CurrencyId))
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
                if (modelVM.AutoId == 0 && modelVM.Tc == 0)
                {
                    var hasSavePermission = await _service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await _service.SaveSetupAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.CurrencyId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await _service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await _service.UpdateSetupAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.CurrencyId });
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
            if (await _service.IsExistByNameAsync(name, code))
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

            var hasPermission = await _service.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await _service.DeleteTab(ids, model);
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
                var list = await _service.GetAllAsync() ?? new List<CurrencyTypeVM>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<CurrencyTypeVM>(), error = ex.Message });
            }
        }

        #endregion

        #region Legacy Endpoints

        [HttpGet]
        [Route("currency-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "CurrencyId", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    result.Data = new List<CurrencyTypeVM>();
                    result.TotalCount = 0;
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("next-currency-id")]
        public async Task<string> GenerateNewIdAsync()
        {
            return await _service.GenerateNextCode();
        }

        #endregion
    }
}
