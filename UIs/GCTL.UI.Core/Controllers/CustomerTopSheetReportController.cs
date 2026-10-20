using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;
using GCTL.Service.CustomerTopSheetReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerTopSheetReportController : BaseController
    {
        #region Private Fields
        private readonly ICustomerTopSheetReportService _reportService;
        #endregion

        #region Constructor
        public CustomerTopSheetReportController(ICustomerTopSheetReportService reportService) : base()
        {
            _reportService = reportService;
        }
        #endregion

        #region Index Action
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync();

                ViewBag.Companies = new SelectList(options.Companies, "Code", "Name");
                ViewBag.Branches = new SelectList(options.Branches, "Code", "Name");
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.JobNos = new SelectList(options.JobNos);
                ViewBag.BillNos = new SelectList(options.BillNos);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing CustomerTopSheetReport Index");
                ViewBag.Companies = new SelectList(new List<CommonSelectModel>());
                ViewBag.Branches = new SelectList(new List<CommonSelectModel>());
                ViewBag.ShipmentModes = new SelectList(new List<CommonSelectModel>());
                ViewBag.Customers = new SelectList(new List<CommonSelectModel>());
                ViewBag.JobNos = new SelectList(new List<string>());
                ViewBag.BillNos = new SelectList(new List<string>());
            }

            var gridModel = CreateGridModel(Url.Action("GetGridData", "CustomerTopSheetReport")!);
            return View(gridModel);
        }
        #endregion

        #region Get Grid Data (DataTables AJAX)
        [HttpPost]
        public async Task<IActionResult> GetGridData(CustomerTopSheetFilter filter)
        {
            try
            {
                filter ??= new CustomerTopSheetFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (!string.IsNullOrEmpty(form["companyCode"])) filter.CompanyCode = form["companyCode"];
                    if (!string.IsNullOrEmpty(form["branchCode"])) filter.BranchCode = form["branchCode"];
                    if (!string.IsNullOrEmpty(form["fromDate"])) filter.FromDate = form["fromDate"];
                    if (!string.IsNullOrEmpty(form["toDate"])) filter.ToDate = form["toDate"];
                    if (!string.IsNullOrEmpty(form["customerId"])) filter.CustomerId = form["customerId"];
                    if (!string.IsNullOrEmpty(form["shipmentModeId"])) filter.ShipmentModeId = form["shipmentModeId"];
                    if (!string.IsNullOrEmpty(form["jobNo"])) filter.JobNo = form["jobNo"];
                    if (!string.IsNullOrEmpty(form["billNo"])) filter.BillNo = form["billNo"];

                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    if (!string.IsNullOrEmpty(form["searchTerm"])) filter.SearchTerm = form["searchTerm"];
                    else if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

                    var sortColIdx = form["order[0][column]"];
                    var sortDir = form["order[0][dir]"];
                    if (!string.IsNullOrEmpty(form["sortColumn"])) filter.SortColumn = form["sortColumn"];
                    else if (!string.IsNullOrEmpty(sortColIdx)) filter.SortColumn = form[$"columns[{sortColIdx}][data]"];

                    if (!string.IsNullOrEmpty(form["sortOrder"])) filter.SortOrder = form["sortOrder"];
                    else if (!string.IsNullOrEmpty(sortDir)) filter.SortOrder = sortDir;
                }

                var result = await _reportService.GetGridDataAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<CustomerTopSheetBillItemDTO>(),
                    summary = result.Summary
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in GetGridData in CustomerTopSheetReportController");
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<CustomerTopSheetBillItemDTO>(),
                    summary = new CustomerTopSheetSummaryDTO()
                });
            }
        }
        #endregion

        #region Get Summary (KPI Stats for Web Dashboard)
        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] CustomerTopSheetFilter filter)
        {
            try
            {
                filter ??= new CustomerTopSheetFilter();
                var summary = await _reportService.GetSummaryAsync(filter);
                return Json(summary);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in GetSummary in CustomerTopSheetReportController");
                return Json(new CustomerTopSheetSummaryDTO());
            }
        }
        #endregion

        #region Cascading Dropdowns
        [HttpGet]
        public async Task<IActionResult> GetBranches(string? companyCode)
        {
            try
            {
                var branches = await _reportService.GetBranchesAsync(companyCode);
                return Json(branches);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading Branches");
                return Json(new List<CommonSelectModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetJobNos(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                var jobs = await _reportService.GetJobNosAsync(customerId, shipmentModeId, fromDate, toDate);
                return Json(jobs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading Job Nos");
                return Json(new List<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBillNos(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                var bills = await _reportService.GetBillNosAsync(customerId, shipmentModeId, fromDate, toDate);
                return Json(bills);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading Bill Nos");
                return Json(new List<string>());
            }
        }
        #endregion

        #region Preview PDF (Inline stream for browser iframe)
        [HttpGet]
        public async Task<IActionResult> PreviewPdf([FromQuery] CustomerTopSheetFilter filter)
        {
            try
            {
                string printUser = User.Identity?.Name ?? "User";
                var pdfBytes = await _reportService.GeneratePdfReportAsync(filter, printUser);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error generating PreviewPdf in CustomerTopSheetReportController");
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Export Actions (PDF, Excel with Tabs, CSV)
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] CustomerTopSheetFilter filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                string printUser = User.Identity?.Name ?? "User";
                var (fileBytes, contentType, fileName) = await _reportService.ExportReportAsync(filter, exportFormat, printUser);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in Export in CustomerTopSheetReportController");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] CustomerTopSheetFilter filter)
        {
            return await Export(filter, "PDF");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] CustomerTopSheetFilter filter)
        {
            return await Export(filter, "Excel");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv([FromQuery] CustomerTopSheetFilter filter)
        {
            return await Export(filter, "CSV");
        }
        #endregion

        #region Private Helper: Grid Model Configuration
        private GridModel CreateGridModel(string ajaxUrl)
        {
            return new GridModel
            {
                GridId = "customerTopSheetReportTable",
                AjaxUrl = ajaxUrl,
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 10,
                PageLengths = new[] { 10, 25, 50, 100, -1 },
                Hover = true,
                ShowCheckbox = false,
                ShowPagination = true,
                ShowRowNumbers = false,
                ShowSearchBox = true,
                Striped = true,
                DefaultSortColumn = "billNo",
                DefaultSortDirection = "asc",
                Columns = new List<GridColumn>
                {
                    new() { Data = "slNo", Header = "SL", Width = "30px", Align = "center", Sortable = false },
                    new() { Data = "customerName", Header = "Customer", Width = "140px", Align = "left", SortField = "CustomerName" },
                    new() { Data = "billNo", Header = "Bill No", Width = "115px", Align = "left", SortField = "BillNo" },
                    new() { Data = "lcLcaNo", Header = "LC/LCA No", Width = "100px", Align = "left" },
                    new() { Data = "invoiceNo", Header = "Invoice No", Width = "95px", Align = "left", SortField = "InvoiceNo" },
                    new() { Data = "invoiceDate", Header = "Inv Date", Width = "70px", Align = "center" },
                    new() { Data = "beRegNo", Header = "B/E Reg No", Width = "80px", Align = "left", SortField = "BeRegNo" },
                    new() { Data = "beRegDate", Header = "B/E Date", Width = "70px", Align = "center" },
                    new() { Data = "dtiAssociationServiceCharge", Header = "DTI", Width = "60px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "vatGlobalTax", Header = "Vat/Tax", Width = "65px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "customsMisc", Header = "Cust. Misc", Width = "65px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "warehouseRent", Header = "Wh. Rent", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "labourCharge", Header = "Labour", Width = "65px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "transportBill", Header = "Transport", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "commission", Header = "Commission", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "totalBillTk", Header = "Total (Tk.)", Width = "85px", Align = "right", RenderFn = "formatCurrency" }
                }
            };
        }
        #endregion
    }
}
