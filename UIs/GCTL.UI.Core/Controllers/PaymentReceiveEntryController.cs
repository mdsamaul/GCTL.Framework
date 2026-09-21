using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PaymentReceiveEntry;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.PaymentReceiveEntry;
using GCTL.UI.Core.ViewModels.PaymentReceiveEntry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GCTL.UI.Core.Controllers
{
    public class PaymentReceiveEntryController : BaseController
    {
        private readonly IPaymentReceiveEntryService _paymentReceiveService;
        private readonly IRepository<SalesDefPaymentType> _paymentTypeRepository;
        private readonly IRepository<SalesDefPaymentMode> _paymentModeRepository;
        private readonly IRepository<HrmEmployee> _employeeRepository;
        private readonly ICommonService _commonService;

        public PaymentReceiveEntryController(
            IPaymentReceiveEntryService paymentReceiveService,
            IRepository<SalesDefPaymentType> paymentTypeRepository,
            IRepository<SalesDefPaymentMode> paymentModeRepository,
            IRepository<HrmEmployee> employeeRepository,
            ICommonService commonService)
        {
            _paymentReceiveService = paymentReceiveService;
            _paymentTypeRepository = paymentTypeRepository;
            _paymentModeRepository = paymentModeRepository;
            _employeeRepository = employeeRepository;
            _commonService = commonService;
        }

        #region Index
        public async Task<IActionResult> Index()
        {
            var hasPermission = await _paymentReceiveService.PagePermissionAsync(LoginInfo?.AccessCode ?? string.Empty);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PaymentReceiveEntryPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                await PopulateDropdownsAsync();

                var nextReceiveId = await _paymentReceiveService.GetNextPaymentReceiveNoAsync();
                var nextMrNo = await _paymentReceiveService.GetNextMrNoAsync();

                model.Setup = new PaymentReceiveEntrySetupViewModel
                {
                    PaymentReceiveId = nextReceiveId,
                    PaymentReceiveDate = DateTime.Now,
                    PaymentTypeId = "01",
                    PaymentModeCode = "001",
                    MrNo = nextMrNo,
                    MrDate = DateTime.Now,
                    CreationDate = DateTime.Now.ToString("dd/MM/yyyy")
                };

                model.CustomerList = await _paymentReceiveService.GetCustomerListForPaymentReceiveAsync();
            }
            catch (Exception ex)
            {
                model.Setup = new PaymentReceiveEntrySetupViewModel();
                Console.WriteLine($"Error in Index: {ex.Message}");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }
        #endregion

        #region Setup (GET Partial)
        public async Task<IActionResult> Setup(string? id)
        {
            var model = new PaymentReceiveEntrySetupViewModel();

            if (!string.IsNullOrEmpty(id))
            {
                model = await _paymentReceiveService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.PaymentReceiveId = await _paymentReceiveService.GetNextPaymentReceiveNoAsync();
                model.PaymentReceiveDate = DateTime.Now;
                model.MrNo = await _paymentReceiveService.GetNextMrNoAsync();
                model.MrDate = DateTime.Now;
                model.PaymentTypeId = "01";
                model.PaymentModeCode = "001";
                model.CreationDate = DateTime.Now.ToString("dd/MM/yyyy");
            }

            await PopulateDropdownsAsync(model.PaymentTypeId, model.PaymentModeCode, model.Receivedby);

            return PartialView($"_{nameof(Setup)}", model);
        }
        #endregion

        #region Setup (POST Save/Update)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(PaymentReceiveEntrySetupViewModel modelVM)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelVM.CustomerId))
                {
                    return Json(new { isSuccess = false, message = "Please select a Customer from the list." });
                }

                if (modelVM.Amount == null || modelVM.Amount <= 0)
                {
                    return Json(new { isSuccess = false, message = "Please enter a valid Payment Amount." });
                }

                if (Request.Form.ContainsKey("AITInvoiceNo"))
                {
                    var aitInvoices = Request.Form["AITInvoiceNo"];
                    if (aitInvoices.Count > 0)
                    {
                        var joined = string.Join(", ", aitInvoices.Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (!string.IsNullOrWhiteSpace(joined))
                        {
                            modelVM.AITInvoiceNo = joined;
                        }
                    }
                }

                modelVM.ToAudit(LoginInfo, (modelVM.Tc ?? 0) > 0);
                modelVM.CompanyCode = LoginInfo?.CompanyCode ?? "001";
                modelVM.EmployeeId = LoginInfo?.EmployeeId;

                var isUpdate = (modelVM.Tc != null && modelVM.Tc > 0) || await _paymentReceiveService.IsExistAsync(modelVM.PaymentReceiveId);

                if (modelVM.AITChallanFile != null && modelVM.AITChallanFile.Length > 0)
                {
                    try
                    {
                        var env = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
                        var webRoot = env?.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
                        var uploadFolder = System.IO.Path.Combine(webRoot, "uploads", "aitchallans");
                        if (!System.IO.Directory.Exists(uploadFolder))
                        {
                            System.IO.Directory.CreateDirectory(uploadFolder);
                        }

                        var ext = System.IO.Path.GetExtension(modelVM.AITChallanFile.FileName);
                        var fileName = $"{modelVM.PaymentReceiveId}_AIT_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                        var filePath = System.IO.Path.Combine(uploadFolder, fileName);
                        using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                        {
                            await modelVM.AITChallanFile.CopyToAsync(stream);
                        }
                        modelVM.AITChallanUpload = $"/uploads/aitchallans/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading AIT Challan: {ex.Message}");
                    }
                }
                else if (isUpdate && string.IsNullOrEmpty(modelVM.AITChallanUpload))
                {
                    var existing = await _paymentReceiveService.GetByIdAsync(modelVM.PaymentReceiveId);
                    if (existing != null)
                    {
                        modelVM.AITChallanUpload = existing.AITChallanUpload;
                    }
                }

                modelVM.PaymentReceiveId ??= string.Empty;
                modelVM.CustomerId ??= string.Empty;
                modelVM.AgainstBillList ??= string.Empty;
                modelVM.PaymentTypeId = string.IsNullOrWhiteSpace(modelVM.PaymentTypeId) ? "01" : modelVM.PaymentTypeId;
                modelVM.PaymentModeCode = string.IsNullOrWhiteSpace(modelVM.PaymentModeCode) ? "001" : modelVM.PaymentModeCode;
                modelVM.PaymentReceiveDate ??= DateTime.Now;
                modelVM.MrDate ??= DateTime.Now;
                modelVM.MrNo ??= string.Empty;
                modelVM.AIT ??= 0;
                modelVM.AITChallanNo ??= string.Empty;
                modelVM.AITInvoiceNo ??= string.Empty;
                modelVM.AITChallanUpload ??= string.Empty;
                modelVM.Remarks ??= string.Empty;
                modelVM.Receivedby ??= string.Empty;
                modelVM.CompanyCode = string.IsNullOrWhiteSpace(LoginInfo?.CompanyCode) ? "001" : LoginInfo.CompanyCode;
                modelVM.EmployeeId = LoginInfo?.EmployeeId ?? string.Empty;
                modelVM.BankFeeOnlineCharge ??= 0;

                if (!isUpdate)
                {
                    var hasSavePermission = await _paymentReceiveService.SavePermissionAsync(LoginInfo?.AccessCode ?? string.Empty);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }

                    var result = await _paymentReceiveService.SaveAsync(modelVM);
                    if (result)
                    {
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.PaymentReceiveId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "Failed to save record." });
                    }
                }
                else
                {
                    var hasUpdatePermission = await _paymentReceiveService.UpdatePermissionAsync(LoginInfo?.AccessCode ?? string.Empty);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }

                    var result = await _paymentReceiveService.UpdateAsync(modelVM);
                    if (result)
                    {
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.PaymentReceiveId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "Failed to update record." });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Setup POST: {ex.Message}");
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
        #endregion

        #region Customer List for Top Grid (Proc_CustomerListForPaymentReceive)
        [HttpGet]
        public async Task<IActionResult> GetCustomerList()
        {
            try
            {
                var list = await _paymentReceiveService.GetCustomerListForPaymentReceiveAsync();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Due Invoices (prc_GetCustomerDueInvoiceList)
        [HttpGet]
        public async Task<IActionResult> GetDueInvoices(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return Json(new { data = new List<CustomerDueInvoiceViewModel>() });
                }

                var list = await _paymentReceiveService.GetCustomerDueInvoiceListAsync(customerId);
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Search Payment Received (prc_LoadGridPaymentReceived)
        [HttpGet]
        public async Task<IActionResult> SearchPaymentReceived(string? fromDate, string? toDate)
        {
            try
            {
                DateTime dtFrom = DateTime.Now.AddMonths(-1);
                DateTime dtTo = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(fromDate))
                {
                    if (DateTime.TryParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
                        dtFrom = f;
                    else if (DateTime.TryParse(fromDate, out var f2))
                        dtFrom = f2;
                }

                if (!string.IsNullOrWhiteSpace(toDate))
                {
                    if (DateTime.TryParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                        dtTo = t;
                    else if (DateTime.TryParse(toDate, out var t2))
                        dtTo = t2;
                }

                var list = await _paymentReceiveService.GetGridPaymentReceivedAsync(dtFrom, dtTo);
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Payment History Summary (Prc_Api_PaymentHistorySummery)
        [HttpGet]
        public async Task<IActionResult> GetPaymentHistorySummary(string customerCode)
        {
            try
            {
                var summary = await _paymentReceiveService.GetPaymentHistorySummaryAsync(customerCode);
                return Json(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region Payment List (Prc_Api_PaymentList)
        [HttpGet]
        public async Task<IActionResult> GetPaymentList(string customerCode, string? fromDate, string? toDate, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                DateTime dtFrom = DateTime.Now.AddYears(-5);
                DateTime dtTo = DateTime.Now.AddYears(1);

                if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var f))
                    dtFrom = f;
                if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var t))
                    dtTo = t;

                var list = await _paymentReceiveService.GetPaymentListAsync(customerCode, dtFrom, dtTo, pageIndex, pageSize);
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region GetById
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await _paymentReceiveService.GetByIdAsync(code);
            return Json(result);
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

            var hasPermission = await _paymentReceiveService.DeletePermissionAsync(LoginInfo?.AccessCode ?? string.Empty);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            var model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo?.CompanyCode;

            var result = await _paymentReceiveService.DeleteTab(ids, model);
            return Json(new { success = result.succses, message = result.messege });
        }
        #endregion

        #region Next Numbers
        [HttpGet]
        public async Task<IActionResult> GetNextNumbers()
        {
            var nextReceiveId = await _paymentReceiveService.GetNextPaymentReceiveNoAsync();
            var nextMrNo = await _paymentReceiveService.GetNextMrNoAsync();
            return Json(new
            {
                paymentReceiveId = nextReceiveId,
                mrNo = nextMrNo
            });
        }
        #endregion

        #region Helper Dropdowns
        private async Task PopulateDropdownsAsync(string? paymentTypeId = null, string? paymentModeCode = null, string? employeeId = null)
        {
            var paymentTypes = await _paymentTypeRepository.All().AsNoTracking()
                .OrderBy(p => p.PaymentTypeId)
                .Select(p => new { id = p.PaymentTypeId, name = p.PaymentType })
                .ToListAsync();

            var paymentModes = await _paymentModeRepository.All().AsNoTracking()
                .OrderBy(p => p.PaymentModeId)
                .Select(p => new { id = p.PaymentModeId, name = p.PaymentModeName })
                .ToListAsync();

            var employees = await _employeeRepository.All().AsNoTracking()
                .OrderBy(e => e.FirstName)
                .Select(e => new { id = e.EmployeeId, name = (e.FirstName + " " + (e.LastName ?? "")).Trim() })
                .ToListAsync();

            ViewBag.PaymentTypes = new SelectList(paymentTypes, "id", "name", paymentTypeId ?? "01");
            ViewBag.PaymentModes = new SelectList(paymentModes, "id", "name", paymentModeCode ?? "001");
            ViewBag.Employees = new SelectList(employees, "id", "name", employeeId);
        }
        #endregion
    }
}
