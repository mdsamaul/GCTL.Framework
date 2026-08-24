using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentType;
using GCTL.Service.Common;
using GCTL.Service.PaymentType;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.PaymentType;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class PaymentTypeController : BaseController
    {
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly ICommonService _commonService;
        private string strMaxNO = string.Empty;

        public PaymentTypeController(
            IPaymentTypeService paymentTypeService,
            ICommonService commonService)
        {
            _paymentTypeService = paymentTypeService;
            _commonService = commonService;
        }

        private async Task<string> GetNextCodeAsync()
        {
            string strMaxNo = string.Empty;
            try
            {
                _commonService.FindMaxNo(ref strMaxNo, "PaymentTypeID", "Sales_Def_PaymentType", 3);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(strMaxNo) || strMaxNo == "000")
            {
                var list = await _paymentTypeService.GetAllAsync();
                if (list != null && list.Any())
                {
                    var maxVal = list.Select(x => int.TryParse(x.PaymentTypeId, out int v) ? v : 0).Max();
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
            var hasPermission = await _paymentTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PaymentTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await _paymentTypeService.GetAllAsync() ?? new List<PaymentTypeSetupViewModel>();
                model.PaymentList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.PaymentTypeId != null && x.PaymentTypeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.PaymentType != null && x.PaymentType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
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
                    headers: new List<string> { "Code", "Payment Type Name", "Short Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "PaymentTypeId", item.PaymentTypeId },
                        { "Payment Type Name", item.PaymentType },
                        { "Short Name", item.ShortName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "PaymentType") ?? "/PaymentType/GetTableData",
                    primaryKey: "PaymentTypeId",
                    hasCheckbox: true,
                    actionUrlPattern: "/PaymentType/Setup/{id}",
                    gridId: "paymentTypeGridContainer",
                    editClass: "js-paymenttype-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "PaymentTypeId", "center" },
                        { "Payment Type Name", "left" },
                        { "Short Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Payment Type Name", "auto" },
                        { "Short Name", "150px" }
                    }
                );

                model.Setup = new PaymentTypeSetupViewModel
                {
                    PaymentTypeId = await GetNextCodeAsync()
                };
            }
            catch (Exception ex)
            {
                model.PaymentList = new List<PaymentTypeSetupViewModel>();
                model.Setup = new PaymentTypeSetupViewModel();
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
            var model = new PaymentTypeSetupViewModel();
            var nextCode = await GetNextCodeAsync();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await _paymentTypeService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.PaymentTypeId = nextCode;
                }
            }
            else
            {
                model.PaymentTypeId = nextCode;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new PaymentTypePageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await _paymentTypeService.GetAllAsync() ?? new List<PaymentTypeSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Payment Type Name", "Short Name" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "PaymentTypeId", item.PaymentTypeId },
                    { "Payment Type Name", item.PaymentType },
                    { "Short Name", item.ShortName }
                },
                ajaxUrl: Url.Action("GetTableData", "PaymentType") ?? "/PaymentType/GetTableData",
                primaryKey: "PaymentTypeId",
                hasCheckbox: true,
                actionUrlPattern: "/PaymentType/Setup/{id}",
                gridId: "paymentTypeGridContainer",
                editClass: "js-paymenttype-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "PaymentTypeId", "center" },
                    { "Payment Type Name", "left" },
                    { "Short Name", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "100px" },
                    { "Payment Type Name", "auto" },
                    { "Short Name", "150px" }
                }
            );

            return View("Index", pageModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(PaymentTypeSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    if (await _paymentTypeService.IsExistAsync(modelVM.PaymentType, modelVM.PaymentTypeId))
                    {
                        return Json(new { isSuccess = false, message = "Payment Type Name already exists.", isDuplicate = true });
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
                    var hasSavePermission = await _paymentTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Save." });
                    }

                    var nextCode = await GetNextCodeAsync();
                    modelVM.PaymentTypeId = nextCode;

                    bool result = await _paymentTypeService.SaveAsync(modelVM);
                    if (result)
                    {
                        var newNextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = newNextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Payment Type." });
                }
                else
                {
                    var hasUpdatePermission = await _paymentTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Update." });
                    }

                    bool result = await _paymentTypeService.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Payment Type." });
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

            var hasPermission = await _paymentTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { isSuccess = false, message = "You have no permission to Delete." });
            }

            var deleteHistory = new DeleteHistoryViewModel();
            deleteHistory.ToAudit(LoginInfo);
            deleteHistory.CompanyCode = LoginInfo.CompanyCode;

            var result = await _paymentTypeService.DeleteTab(ids, deleteHistory);
            return Json(new { isSuccess = result.succses, message = result.messege });
        }

        #endregion

        #region GetTableData

        [HttpGet]
        public async Task<IActionResult> GetTableData(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var list = await _paymentTypeService.GetAllAsync() ?? new List<PaymentTypeSetupViewModel>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                list = list.Where(x => (x.PaymentTypeId != null && x.PaymentTypeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                    || (x.PaymentType != null && x.PaymentType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                    || (x.ShortName != null && x.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                           .ToList();
            }

            int totalRecords = list.Count;
            var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var dynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: page,
                pageSize: pageSize,
                headers: new List<string> { "Code", "Payment Type Name", "Short Name" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "PaymentTypeId", item.PaymentTypeId },
                    { "Payment Type Name", item.PaymentType },
                    { "Short Name", item.ShortName }
                },
                ajaxUrl: Url.Action("GetTableData", "PaymentType") ?? "/PaymentType/GetTableData",
                primaryKey: "PaymentTypeId",
                hasCheckbox: true,
                actionUrlPattern: "/PaymentType/Setup/{id}",
                gridId: "paymentTypeGridContainer",
                editClass: "js-paymenttype-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "PaymentTypeId", "center" },
                    { "Payment Type Name", "left" },
                    { "Short Name", "left" }
                },
                searchTerm: searchTerm,
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "100px" },
                    { "Payment Type Name", "auto" },
                    { "Short Name", "150px" }
                }
            );

            return PartialView("_DynamicGrid", dynamicGrid);
        }

        #endregion
    }
}
