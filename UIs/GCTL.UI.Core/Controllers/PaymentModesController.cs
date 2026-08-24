using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentModes;
using GCTL.Service.Common;
using GCTL.Service.PaymentModes;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.PaymentModes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class PaymentModesController : BaseController
    {
        private readonly IPaymentModeService _paymentModeService;
        private readonly ICommonService _commonService;
        private string strMaxNO = string.Empty;

        public PaymentModesController(
            IPaymentModeService paymentModeService,
            ICommonService commonService)
        {
            _paymentModeService = paymentModeService;
            _commonService = commonService;
        }

        private async Task<string> GetNextCodeAsync()
        {
            string strMaxNo = string.Empty;
            try
            {
                _commonService.FindMaxNo(ref strMaxNo, "PaymentModeID", "Sales_Def_PaymentMode", 3);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(strMaxNo) || strMaxNo == "000")
            {
                var list = await _paymentModeService.GetAllAsync();
                if (list != null && list.Any())
                {
                    var maxVal = list.Select(x => int.TryParse(x.PaymentModeId, out int v) ? v : 0).Max();
                    strMaxNo = (maxVal + 1).ToString("D3");
                }
                else
                {
                    strMaxNo = "001";
                }
            }
            return strMaxNo;
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await _paymentModeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PaymentModePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await _paymentModeService.GetAllAsync() ?? new List<PaymentModeSetupViewModel>();
                model.PaymentModeList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.PaymentModeId != null && x.PaymentModeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.PaymentModeName != null && x.PaymentModeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.PaymentModeShortName != null && x.PaymentModeShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Payment Mode Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "PaymentModeId", item.PaymentModeId },
                        { "Payment Mode Name", item.PaymentModeName },
                        { "Short Name", item.PaymentModeShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "PaymentModes") ?? "/PaymentModes/GetTableData",
                    primaryKey: "PaymentModeId",
                    hasCheckbox: true,
                    actionUrlPattern: "/PaymentModes/Setup/{id}",
                    gridId: "paymentModesGridContainer",
                    editClass: "js-paymentmodes-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "PaymentModeId", "center" },
                        { "Payment Mode Name", "left" },
                        { "Short Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Payment Mode Name", "auto" },
                        { "Short Name", "150px" }
                    }
                );

                model.Setup = new PaymentModeSetupViewModel
                {
                    PaymentModeId = await GetNextCodeAsync()
                };
            }
            catch (Exception ex)
            {
                model.PaymentModeList = new List<PaymentModeSetupViewModel>();
                model.Setup = new PaymentModeSetupViewModel();
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
            var model = new PaymentModeSetupViewModel();
            var nextCode = await GetNextCodeAsync();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await _paymentModeService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.PaymentModeId = nextCode;
                }
            }
            else
            {
                model.PaymentModeId = nextCode;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new PaymentModePageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await _paymentModeService.GetAllAsync() ?? new List<PaymentModeSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Payment Mode Name", "Short Name" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "PaymentModeId", item.PaymentModeId },
                    { "Payment Mode Name", item.PaymentModeName },
                    { "Short Name", item.PaymentModeShortName }
                },
                ajaxUrl: Url.Action("GetTableData", "PaymentModes") ?? "/PaymentModes/GetTableData",
                primaryKey: "PaymentModeId",
                hasCheckbox: true,
                actionUrlPattern: "/PaymentModes/Setup/{id}",
                gridId: "paymentModesGridContainer",
                editClass: "js-paymentmodes-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "PaymentModeId", "center" },
                    { "Payment Mode Name", "left" },
                    { "Short Name", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "100px" },
                    { "Payment Mode Name", "auto" },
                    { "Short Name", "150px" }
                }
            );

            return View("Index", pageModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(PaymentModeSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    if (await _paymentModeService.IsPaymentModeExistAsync(modelVM.PaymentModeName, modelVM.PaymentModeId))
                    {
                        return Json(new { isSuccess = false, message = "Payment Mode Name already exists.", isDuplicate = true });
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
                    var hasSavePermission = await _paymentModeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Save." });
                    }

                    var nextCode = await GetNextCodeAsync();
                    modelVM.PaymentModeId = nextCode;

                    bool result = await _paymentModeService.SaveAsync(modelVM);
                    if (result)
                    {
                        var newNextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = newNextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Payment Mode." });
                }
                else
                {
                    var hasUpdatePermission = await _paymentModeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Update." });
                    }

                    bool result = await _paymentModeService.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Payment Mode." });
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

            var hasPermission = await _paymentModeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { isSuccess = false, message = "You have no permission to Delete." });
            }

            var deleteHistory = new DeleteHistoryViewModel();
            deleteHistory.ToAudit(LoginInfo);
            deleteHistory.CompanyCode = LoginInfo.CompanyCode;

            var result = await _paymentModeService.DeleteTab(ids, deleteHistory);
            return Json(new { isSuccess = result.succses, message = result.messege });
        }

        #endregion

        #region GetTableData

        [HttpGet]
        public async Task<IActionResult> GetTableData(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var list = await _paymentModeService.GetAllAsync() ?? new List<PaymentModeSetupViewModel>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                list = list.Where(x => (x.PaymentModeId != null && x.PaymentModeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                    || (x.PaymentModeName != null && x.PaymentModeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                    || (x.PaymentModeShortName != null && x.PaymentModeShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                           .ToList();
            }

            int totalRecords = list.Count;
            var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var dynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: page,
                pageSize: pageSize,
                headers: new List<string> { "Code", "Payment Mode Name", "Short Name" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "PaymentModeId", item.PaymentModeId },
                    { "Payment Mode Name", item.PaymentModeName },
                    { "Short Name", item.PaymentModeShortName }
                },
                ajaxUrl: Url.Action("GetTableData", "PaymentModes") ?? "/PaymentModes/GetTableData",
                primaryKey: "PaymentModeId",
                hasCheckbox: true,
                actionUrlPattern: "/PaymentModes/Setup/{id}",
                gridId: "paymentModesGridContainer",
                editClass: "js-paymentmodes-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "PaymentModeId", "center" },
                    { "Payment Mode Name", "left" },
                    { "Short Name", "left" }
                },
                searchTerm: searchTerm,
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "100px" },
                    { "Payment Mode Name", "auto" },
                    { "Short Name", "150px" }
                }
            );

            return PartialView("_DynamicGrid", dynamicGrid);
        }

        #endregion

        #region Availability Check

        [HttpPost]
        public async Task<IActionResult> CheckAvailability(string name, string code)
        {
            if (await _paymentModeService.IsPaymentModeExistAsync(name, code))
            {
                return Json(new { isSuccess = true, message = "Payment Mode Name already exists." });
            }

            return Json(new { isSuccess = false });
        }

        #endregion
    }
}