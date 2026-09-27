using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Service.BillService;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.ShipmentMode;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers.CNFBill
{
    public class JobBillEntryReportController : BaseController
    {
        private readonly IJobBillEntryReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly ISalesCustomerService _customerService;

        public JobBillEntryReportController(
            IJobBillEntryReportService reportService,
            IShipmentMode shipmentMode,
            ISalesCustomerService customerService) : base()
        {
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _customerService = customerService;
        }

        #region Index View
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var defaultFromDate = DateTime.Now.ToString("01/MM/yyyy");
            var defaultToDate = DateTime.Now.ToString("dd/MM/yyyy");
            await PopulateDropdowns(defaultFromDate, defaultToDate);

            var gridModel = new GridModel
            {
                GridId = "jobBillEntryReportTable",
                AjaxUrl = Url.Action("GetGridData", "JobBillEntryReport"),
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                CheckAllId = "chkSelectAllBills",
                CheckboxClass = "chk-bill-item",
                CheckColumnData = "billNo",
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 10,
                Hover = true,
                ShowCheckbox = true,
                ShowPagination = true,
                ShowRowNumbers = false,
                ShowSearchBox = true,
                Striped = true,
                Columns = new List<GridColumn>
                {
                    new() { Data = "billNo", Header = "Bill No", Width = "7%", Align = "left", SortField = "BillNo" },
                    new() { Data = "billDate", Header = "Date", Width = "6%", Align = "center", SortField = "BillDate" },
                    new() { Data = "jobNo", Header = "Job No", Width = "7%", Align = "left", IsWrapable = true, SortField = "JobNo" },
                    new() { Data = "materialDescription", Header = "Material Desc.", Width = "18%", Align = "left", SortField = "MaterialDescription" },
                    new() { Data = "quantity", Header = "<span class=\"th-full\">Quantity</span><span class=\"th-compact\">Qty</span>", Width = "5%", Align = "right", SortField = "Quantity" },
                    new() { Data = "weight", Header = "Weight", Width = "5%", Align = "right", SortField = "DocWeight" },
                    new() { Data = "receiptableAmount", Header = "<span class=\"th-full\">Receiptable Amt</span><span class=\"th-compact\">Receipt.<br/>Amt</span>", Width = "7%", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "nonReceiptableAmount", Header = "<span class=\"th-full\">Non-Receiptable Amt</span><span class=\"th-compact\">Non-Receipt.<br/>Amt</span>", Width = "8%", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "transportAmount", Header = "Transport", Width = "6%", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "agencyCommission", Header = "<span class=\"th-full\">Agency Comm.</span><span class=\"th-compact\">Agency<br/>Comm.</span>", Width = "6%", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "totalAmount", Header = "<span class=\"th-full\">Total Amt</span><span class=\"th-compact\">Total<br/>Amt</span>", Width = "7%", Align = "right", RenderFn = "formatCurrency", SortField = "BillAmount" },
                    new() { Data = "deduction", Header = "<span class=\"th-full\">Deduction</span><span class=\"th-compact\">Deduct.</span>", Width = "6%", Align = "right", RenderFn = "formatCurrency", SortField = "SpecialDiscount" },
                    new() { Data = "netAmount", Header = "<span class=\"th-full\">Net Amt</span><span class=\"th-compact\">Net<br/>Amt</span>", Width = "8%", Align = "right", RenderFn = "formatCurrency" }
                }
            };

            return View(gridModel);
        }

        private async Task PopulateDropdowns(string? fromDate = null, string? toDate = null)
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync(fromDate, toDate);
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.BillNos = new SelectList(options.BillNos.Select(x => new SelectListItem { Value = x, Text = x }), "Value", "Text");
            }
            catch
            {
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.BillNos = new SelectList(new List<string>());
            }
        }
        #endregion

        #region AJAX Endpoints
        [HttpPost]
        public async Task<IActionResult> GetGridData(JobBillEntryReportFilterVM filter)
        {
            try
            {
                filter ??= new JobBillEntryReportFilterVM();

                // Extract form parameters if available
                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

                    var sortColIdx = form["order[0][column]"];
                    var sortDir = form["order[0][dir]"];
                    if (!string.IsNullOrEmpty(form["sortColumn"]))
                    {
                        filter.SortColumn = form["sortColumn"];
                    }
                    else if (!string.IsNullOrEmpty(sortColIdx))
                    {
                        var colDataName = form[$"columns[{sortColIdx}][data]"];
                        filter.SortColumn = colDataName;
                    }

                    if (!string.IsNullOrEmpty(form["sortOrder"]))
                    {
                        filter.SortOrder = form["sortOrder"];
                    }
                    else if (!string.IsNullOrEmpty(sortDir))
                    {
                        filter.SortOrder = sortDir;
                    }
                }

                var result = await _reportService.GetGridDataAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<JobBillEntryReportItemDTO>()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<JobBillEntryReportItemDTO>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] JobBillEntryReportFilterVM filter)
        {
            try
            {
                filter ??= new JobBillEntryReportFilterVM();
                var summary = await _reportService.GetSummaryAsync(filter);

                return Json(new
                {
                    success = true,
                    total = summary.TotalBillCount,
                    netServiceValue = summary.NetServiceValue,
                    totalDue = summary.TotalDueOutstanding,
                    lastServiceValue = summary.LastServiceValue
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    total = 0,
                    netServiceValue = 0m,
                    totalDue = 0m,
                    lastServiceValue = 0m
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions(
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? shipmentModes = null,
            [FromQuery] string? customerIds = null)
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync(fromDate, toDate, shipmentModes, customerIds);
                return Json(new
                {
                    success = true,
                    shipmentModes = options.ShipmentModes.Select(x => new { value = x.Code, text = x.Name }),
                    customers = options.Customers.Select(x => new { value = x.Code, text = x.Name }),
                    billNos = options.BillNos.Select(x => new { value = x, text = x })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    shipmentModes = new List<object>(),
                    customers = new List<object>(),
                    billNos = new List<object>()
                });
            }
        }
        #endregion

        #region PDF Export
        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] JobBillEntryReportFilterVM filter)
        {
            try
            {
                filter ??= new JobBillEntryReportFilterVM();
                var pdfBytes = await _reportService.GeneratePdfAsync(filter);
                Response.Headers.Append("Content-Disposition", $"inline; filename=JobBillReport_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Report export failed: {ex.Message}");
            }
        }
        #endregion
    }
}
