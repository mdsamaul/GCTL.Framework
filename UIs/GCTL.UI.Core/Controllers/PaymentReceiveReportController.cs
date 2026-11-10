using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.PaymentReceiveReport;
using GCTL.Service.PaymentReceiveReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class PaymentReceiveReportController : BaseController
    {
        private readonly IPaymentReceiveReportService _paymentReceiveService;

        public PaymentReceiveReportController(IPaymentReceiveReportService paymentReceiveService) : base()
        {
            _paymentReceiveService = paymentReceiveService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var vm = await _paymentReceiveService.GetFilterOptionsAsync();
                ViewBag.CustomerTypes = vm.CustomerTypes;
                ViewBag.Districts = vm.Districts;
                ViewBag.Thanas = vm.Thanas;
                ViewBag.Employees = vm.Employees;
                ViewBag.PaymentTypes = vm.PaymentTypes;
                ViewBag.PaymentModes = vm.PaymentModes;
                ViewBag.PaymentStatuses = vm.PaymentStatuses;
                ViewBag.ExportFormats = new SelectList(new List<string> { "Preview", "PDF", "Excel" });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing PaymentReceiveReport Index");
                ViewBag.CustomerTypes = new List<SelectListItem>();
                ViewBag.Districts = new List<SelectListItem>();
                ViewBag.Thanas = new List<SelectListItem>();
                ViewBag.Employees = new List<SelectListItem>();
                ViewBag.PaymentTypes = new List<SelectListItem>();
                ViewBag.PaymentModes = new List<SelectListItem>();
                ViewBag.PaymentStatuses = new List<SelectListItem>();
                ViewBag.ExportFormats = new SelectList(new List<string> { "Preview", "PDF", "Excel" });
            }

            var gridModel = CreateGridModel(Url.Action("GetGridData", "PaymentReceiveReport")!);
            return View(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetGridData(PaymentReceiveReportFilter filter)
        {
            try
            {
                filter ??= new PaymentReceiveReportFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (!string.IsNullOrEmpty(form["fromDate"])) filter.FromDate = form["fromDate"];
                    if (!string.IsNullOrEmpty(form["toDate"])) filter.ToDate = form["toDate"];
                    if (!string.IsNullOrEmpty(form["customerTypeId"])) filter.CustomerTypeId = form["customerTypeId"];
                    if (!string.IsNullOrEmpty(form["district"])) filter.District = form["district"];
                    if (!string.IsNullOrEmpty(form["employeeId"])) filter.EmployeeId = form["employeeId"];
                    if (!string.IsNullOrEmpty(form["paymentTypeId"])) filter.PaymentTypeId = form["paymentTypeId"];
                    if (!string.IsNullOrEmpty(form["paymentModeId"])) filter.PaymentModeId = form["paymentModeId"];
                    if (!string.IsNullOrEmpty(form["paymentStatus"])) filter.PaymentStatus = form["paymentStatus"];
                }

                var (items, summary) = await _paymentReceiveService.GetGridDataAsync(filter);
                return Json(new
                {
                    draw = Request.HasFormContentType && int.TryParse(Request.Form["draw"], out int d) ? d : 1,
                    recordsTotal = items?.Count ?? 0,
                    recordsFiltered = items?.Count ?? 0,
                    data = items ?? new List<PaymentReceiveReportItemDTO>(),
                    summary = summary ?? new PaymentReceiveReportSummaryDTO()
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching grid data in PaymentReceiveReportController");
                return Json(new
                {
                    draw = Request.HasFormContentType && int.TryParse(Request.Form["draw"], out int d) ? d : 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<PaymentReceiveReportItemDTO>(),
                    summary = new PaymentReceiveReportSummaryDTO()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPdf([FromQuery] PaymentReceiveReportFilter filter)
        {
            try
            {
                var pdfBytes = await _paymentReceiveService.GeneratePdfReportAsync(filter);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error generating PreviewPdf in PaymentReceiveReportController");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] PaymentReceiveReportFilter filter, [FromQuery] string format)
        {
            try
            {
                if (string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
                {
                    var excelBytes = await _paymentReceiveService.ExportExcelAsync(filter);
                    string fileName = $"PaymentReceiveReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

                var pdfBytes = await _paymentReceiveService.GeneratePdfReportAsync(filter);
                string pdfFileName = $"PaymentReceiveReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error exporting file in PaymentReceiveReportController");
                return BadRequest(new { message = ex.Message });
            }
        }

        private GridModel CreateGridModel(string loadUrl)
        {
            return new GridModel
            {
                GridId = "paymentReceiveReportTable",
                AjaxUrl = loadUrl,
                AjaxMethod = "POST",
                ServerSide = true,
                DefaultPageLength = 10,
                Bordered = true,
                Hover = true,
                ShowCheckbox = false,
                ShowPagination = true,
                ShowSearchBox = true,
                ShowRowNumbers = false,
                ShowSkeletonLoading = false,
                Striped = true,
                Columns = new List<GridColumn>
                {
                    new GridColumn { Data = "slNo", Header = "SL", Width = "30px", Align = "center" },
                    new GridColumn { Data = "paymentReceiveNo", Header = "Payment Receive No.", Width = "110px", Align = "center" },
                    new GridColumn { Data = "paymentReceiveDate", Header = "Date", Width = "75px", Align = "center" },
                    new GridColumn { Data = "customerID", Header = "Customer ID", Width = "80px", Align = "center" },
                    new GridColumn { Data = "customer", Header = "Customer", Width = "150px", Align = "left" },
                    new GridColumn { Data = "customerType", Header = "Customer Type", Width = "90px", Align = "center" },
                    new GridColumn { Data = "billingAddress", Header = "Billing Address", Width = "160px", Align = "left" },
                    new GridColumn { Data = "employee", Header = "Employee", Width = "110px", Align = "left" },
                    new GridColumn { Data = "invoiceNoList", Header = "Invoice No./List", Width = "130px", Align = "left", RenderFn = "renderInvoiceList" },
                    new GridColumn { Data = "paymentType", Header = "Payment Type", Width = "85px", Align = "center" },
                    new GridColumn { Data = "paymentMode", Header = "Payment Mode", Width = "85px", Align = "center" },
                    new GridColumn { Data = "paymentAmount", Header = "Payment Amount", Width = "95px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "bankFeeOnlineCharge", Header = "Bank Fee", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "chequeNo", Header = "Cheque No.", Width = "80px", Align = "center" },
                    new GridColumn { Data = "chequeDate", Header = "Cheque Date", Width = "75px", Align = "center" },
                    new GridColumn { Data = "depositBankAccount", Header = "Deposit Bank A/C", Width = "150px", Align = "left" },
                    new GridColumn { Data = "maturityDate", Header = "Maturity Date", Width = "75px", Align = "center" }
                }
            };
        }
    }
}

