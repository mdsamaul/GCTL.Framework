using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentTerms;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.PaymentTerms;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.PaymentTerms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class PaymentTermsController : BaseController
    {
        private readonly IPaymentTermsService _paymentTermsService;
        private readonly IRepository<SalesDefPaymentType> _paymentTypeRepository;
        private readonly ICommonService _commonService;
        private string strMaxNO = string.Empty;

        public PaymentTermsController(
            IPaymentTermsService paymentTermsService,
            IRepository<SalesDefPaymentType> paymentTypeRepository,
            ICommonService commonService)
        {
            _paymentTermsService = paymentTermsService;
            _paymentTypeRepository = paymentTypeRepository;
            _commonService = commonService;
        }

        private async Task<string> GetNextCodeAsync()
        {
            string strMaxNo = string.Empty;
            try
            {
                _commonService.FindMaxNo(ref strMaxNo, "PaymentTermsId", "Sales_Def_PaymentTerms", 3);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(strMaxNo) || strMaxNo == "000")
            {
                var list = await _paymentTermsService.GetAllAsync();
                if (list != null && list.Any())
                {
                    var maxVal = list.Select(x => int.TryParse(x.PaymentTermsId, out int v) ? v : 0).Max();
                    strMaxNo = (maxVal + 1).ToString("D3");
                }
                else
                {
                    strMaxNo = "001";
                }
            }
            return strMaxNo;
        }

        private void PopulateDropdowns()
        {
            ViewBag.TypeDD = new SelectList(_paymentTypeRepository.All(), "PaymentTypeId", "PaymentType");
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await _paymentTermsService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PaymentTermsPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                PopulateDropdowns();

                var list = await _paymentTermsService.GetAllAsync() ?? new List<PaymentTermsSetupViewModel>();
                model.PaymentTermsList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.PaymentTermsId != null && x.PaymentTermsId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.PaymentTermsName != null && x.PaymentTermsName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.PaymentType != null && x.PaymentType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Type", "Percentage", "Credit Days", "Payment Terms Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "PaymentTermsId", item.PaymentTermsId },
                        { "Type", item.PaymentType ?? item.Type },
                        { "Percentage", item.Percentise },
                        { "Credit Days", item.CreditDays },
                        { "Payment Terms Name", item.PaymentTermsName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "PaymentTerms") ?? "/PaymentTerms/GetTableData",
                    primaryKey: "PaymentTermsId",
                    hasCheckbox: true,
                    actionUrlPattern: "/PaymentTerms/Setup/{id}",
                    gridId: "paymentTermsGridContainer",
                    editClass: "js-paymentterms-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "PaymentTermsId", "center" },
                        { "Type", "left" },
                        { "Percentage", "center" },
                        { "Credit Days", "center" },
                        { "Payment Terms Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "90px" },
                        { "Type", "150px" },
                        { "Percentage", "110px" },
                        { "Credit Days", "110px" },
                        { "Payment Terms Name", "auto" }
                    }
                );

                model.Setup = new PaymentTermsSetupViewModel
                {
                    PaymentTermsId = await GetNextCodeAsync()
                };
            }
            catch (Exception ex)
            {
                model.PaymentTermsList = new List<PaymentTermsSetupViewModel>();
                model.Setup = new PaymentTermsSetupViewModel();
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
            var model = new PaymentTermsSetupViewModel();
            var nextCode = await GetNextCodeAsync();
            PopulateDropdowns();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await _paymentTermsService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.PaymentTermsId = nextCode;
                }
            }
            else
            {
                model.PaymentTermsId = nextCode;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new PaymentTermsPageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await _paymentTermsService.GetAllAsync() ?? new List<PaymentTermsSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Type", "Percentage", "Credit Days", "Payment Terms Name" },
                rowMapper: item => new Dictionary<string, object?>
                {
                    { "PaymentTermsId", item.PaymentTermsId },
                    { "Type", item.PaymentType ?? item.Type },
                    { "Percentage", item.Percentise },
                    { "Credit Days", item.CreditDays },
                    { "Payment Terms Name", item.PaymentTermsName }
                },
                ajaxUrl: Url.Action("GetTableData", "PaymentTerms") ?? "/PaymentTerms/GetTableData",
                primaryKey: "PaymentTermsId",
                hasCheckbox: true,
                actionUrlPattern: "/PaymentTerms/Setup/{id}",
                gridId: "paymentTermsGridContainer",
                editClass: "js-paymentterms-edit",
                columnAlignments: new Dictionary<string, string>
                {
                    { "PaymentTermsId", "center" },
                    { "Type", "left" },
                    { "Percentage", "center" },
                    { "Credit Days", "center" },
                    { "Payment Terms Name", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
                    { "SelectAll", "120px" },
                    { "Code", "90px" },
                    { "Type", "150px" },
                    { "Percentage", "110px" },
                    { "Credit Days", "110px" },
                    { "Payment Terms Name", "auto" }
                }
            );

            return View("Index", pageModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(PaymentTermsSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    if (await _paymentTermsService.IsExistAsync(modelVM.PaymentTermsName ?? "", modelVM.PaymentTermsId ?? ""))
                    {
                        return Json(new { isSuccess = false, message = "Payment Terms Name already exists.", isDuplicate = true });
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
                    var hasSavePermission = await _paymentTermsService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Save." });
                    }

                    var nextCode = await GetNextCodeAsync();
                    modelVM.PaymentTermsId = nextCode;

                    bool result = await _paymentTermsService.SaveAsync(modelVM);
                    if (result)
                    {
                        var newNextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = newNextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Payment Terms." });
                }
                else
                {
                    var hasUpdatePermission = await _paymentTermsService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Update." });
                    }

                    bool result = await _paymentTermsService.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Payment Terms." });
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

            var hasPermission = await _paymentTermsService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { isSuccess = false, message = "You have no permission to Delete." });
            }

            var deleteHistory = new DeleteHistoryViewModel();
            deleteHistory.ToAudit(LoginInfo);
            deleteHistory.CompanyCode = LoginInfo.CompanyCode;

            var result = await _paymentTermsService.DeleteTab(ids, deleteHistory);
            return Json(new { isSuccess = result.succses, message = result.messege });
        }

        #endregion

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _paymentTermsService.GetAllAsync() ?? new List<PaymentTermsSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<PaymentTermsSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion
    }
}
