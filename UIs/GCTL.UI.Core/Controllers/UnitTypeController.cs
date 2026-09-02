using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.UnitType;
using GCTL.Service.Common;
using GCTL.Service.UnitType;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.UnitType;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Controllers
{
    public class UnitTypeController : BaseController
    {
        private readonly IUnitType _service;
        private readonly ICommonService _commonService;

        public UnitTypeController(IUnitType service, ICommonService commonService) : base()
        {
            _service = service;
            _commonService = commonService;
        }

        private async Task<string> GetNextCodeAsync()
        {
            string strMaxNO = string.Empty;
            try
            {
                _commonService.FindMaxNo(ref strMaxNO, "UnitTypID", "RMG_Prod_Def_UnitType", 3);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(strMaxNO) || strMaxNO == "000")
            {
                var lastId = await _service.GetLastUnitAsync();
                if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId, out int lastNumericId))
                {
                    strMaxNO = (lastNumericId + 1).ToString("D3");
                }
                else
                {
                    strMaxNO = "001";
                }
            }
            return strMaxNO;
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await _service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new UnitTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await _service.GetAllAsync() ?? new List<UnitTypeSetupViewModel>();
                model.TableList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.UnitTypID != null && x.UnitTypID.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.UnitTypeName != null && x.UnitTypeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
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
                    headers: new List<string> { "Code", "Unit Type Name", "Short Name", "Decimal Places" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "UnitTypID", item.UnitTypID },
                        { "Unit Type Name", item.UnitTypeName },
                        { "Short Name", item.ShortName },
                        { "Decimal Places", item.DecimalPlaces }
                    },
                    ajaxUrl: Url.Action("GetTableData", "UnitType") ?? "/UnitType/GetTableData",
                    primaryKey: "UnitTypID",
                    hasCheckbox: true,
                    actionUrlPattern: "/UnitType/Setup/{id}",
                    gridId: "unitTypeGridContainer",
                    editClass: "js-unittype-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "UnitTypID", "center" },
                        { "Unit Type Name", "left" },
                        { "Short Name", "left" },
                        { "Decimal Places", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Unit Type Name", "auto" },
                        { "Short Name", "150px" },
                        { "Decimal Places", "120px" }
                    }
                );

                model.Setup = new UnitTypeSetupViewModel
                {
                    UnitTypID = await GetNextCodeAsync()
                };
            }
            catch (Exception ex)
            {
                model.TableList = new List<UnitTypeSetupViewModel>();
                model.Setup = new UnitTypeSetupViewModel();
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
            var model = new UnitTypeSetupViewModel();
            var nextCode = await GetNextCodeAsync();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await _service.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.UnitTypID = nextCode;
                }
            }
            else
            {
                model.UnitTypID = nextCode;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new UnitTypePageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await _service.GetAllAsync() ?? new List<UnitTypeSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Unit Type Name", "Short Name", "Decimal Places" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "UnitTypID", item.UnitTypID },
                    { "Unit Type Name", item.UnitTypeName },
                    { "Short Name", item.ShortName },
                    { "Decimal Places", item.DecimalPlaces }
                },
                ajaxUrl: Url.Action("GetTableData", "UnitType") ?? "/UnitType/GetTableData",
                primaryKey: "UnitTypID",
                hasCheckbox: true,
                actionUrlPattern: "/UnitType/Setup/{id}",
                gridId: "unitTypeGridContainer",
                editClass: "js-unittype-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "UnitTypID", "center" },
                    { "Unit Type Name", "left" },
                    { "Short Name", "left" },
                    { "Decimal Places", "center" }
                },
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "100px" },
                    { "Unit Type Name", "auto" },
                    { "Short Name", "150px" },
                    { "Decimal Places", "120px" }
                }
            );

            return View("Index", pageModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(UnitTypeSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    if (await _service.IsDuplicateAsync(modelVM))
                    {
                        return Json(new { isSuccess = false, message = "Unit Type Name already exists.", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, (modelVM.AutoId > 0 || modelVM.Tc > 0));

                if (modelVM.Tc == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    var hasSavePermission = await _service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Save." });
                    }

                    var nextCode = await GetNextCodeAsync();
                    modelVM.UnitTypID = nextCode;

                    bool result = await _service.SaveAsync(modelVM);
                    if (result)
                    {
                        var newNextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = newNextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Unit Type." });
                }
                else
                {
                    var hasUpdatePermission = await _service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Update." });
                    }

                    bool result = await _service.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Unit Type." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { isSuccess = false, message = "An error occurred while saving." });
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { isSuccess = false, message = "No records selected to delete." });
            }

            var hasPermission = await _service.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { isSuccess = false, message = "You have no permission to Delete." });
            }

            var deleteHistory = new DeleteHistoryViewModel();
            deleteHistory.ToAudit(LoginInfo);
            deleteHistory.CompanyCode = LoginInfo.CompanyCode;

            var result = await _service.DeleteTab(ids, deleteHistory);
            return Json(new { isSuccess = result.succses, message = result.messege });
        }

        #endregion

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _service.GetAllAsync() ?? new List<UnitTypeSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<UnitTypeSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion
    }
}
