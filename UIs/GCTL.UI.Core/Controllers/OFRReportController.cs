using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;
using GCTL.Service.OFRReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class OFRReportController : BaseController
    {
        #region Private Fields
        private readonly IOFRReportService _ofrReportService;
        #endregion

        #region Constructor
        public OFRReportController(IOFRReportService ofrReportService) : base()
        {
            _ofrReportService = ofrReportService;
        }
        #endregion

        #region Index Action
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var options = await _ofrReportService.GetFilterOptionsAsync();

                ViewBag.Companies = new SelectList(options.Companies, "Code", "Name");
                ViewBag.Branches = new SelectList(options.Branches, "Code", "Name");
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.ServiceProviders = new SelectList(options.ServiceProviders, "Code", "Name");
                ViewBag.ReportTypes = new SelectList(options.ReportTypes);

                // Initial job numbers
                var initialJobs = await _ofrReportService.GetJobNosAsync(null, null, null, null);
                ViewBag.JobNos = new SelectList(initialJobs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing OFRReport Index");
                ViewBag.Companies = new SelectList(new List<string>());
                ViewBag.Branches = new SelectList(new List<string>());
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.ServiceProviders = new SelectList(new List<string>());
                ViewBag.ReportTypes = new SelectList(new List<string> { "OFR Report", "OFR Approval Report", "OFR Final Report" });
                ViewBag.JobNos = new SelectList(new List<string>());
            }

            var gridModel = CreateGridModel(Url.Action("GetGridData", "OFRReport")!);
            return View(gridModel);
        }
        #endregion

        #region Get Grid Data (DataTables AJAX)
        [HttpPost]
        public async Task<IActionResult> GetGridData(OFRReportFilter filter)
        {
            try
            {
                filter ??= new OFRReportFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (!string.IsNullOrEmpty(form["companyCode"])) filter.CompanyCode = form["companyCode"];
                    if (!string.IsNullOrEmpty(form["branchCode"])) filter.BranchCode = form["branchCode"];
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

                var result = await _ofrReportService.GetGridDataAsync(filter);
                var summary = await _ofrReportService.GetSummaryAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<OFRReportGridRowDTO>(),
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in GetGridData in OFRReportController");
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<OFRReportGridRowDTO>(),
                    summary = new OFRReportSummaryDTO()
                });
            }
        }
        #endregion

        #region Get Summary (KPI Stats)
        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] OFRReportFilter filter)
        {
            try
            {
                filter ??= new OFRReportFilter();
                var summary = await _ofrReportService.GetSummaryAsync(filter);
                return Json(summary);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in GetSummary in OFRReportController");
                return Json(new OFRReportSummaryDTO());
            }
        }
        #endregion

        #region Cascading Dropdown: Branches
        [HttpGet]
        public async Task<IActionResult> GetBranches(string? companyCode)
        {
            try
            {
                var branches = await _ofrReportService.GetBranchesAsync(companyCode);
                return Json(branches);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading Branches");
                return Json(new List<CommonSelectModel>());
            }
        }
        #endregion

        #region Cascading Dropdown: Job Nos
        [HttpGet]
        public async Task<IActionResult> GetJobNos(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                var jobs = await _ofrReportService.GetJobNosAsync(customerId, shipmentModeId, fromDate, toDate);
                return Json(jobs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading Job Nos");
                return Json(new List<string>());
            }
        }
        #endregion

        #region Preview PDF (Inline stream for browser iframe)
        [HttpGet]
        public async Task<IActionResult> PreviewPdf([FromQuery] OFRReportFilter filter)
        {
            try
            {
                string printUser = User.Identity?.Name ?? "shariful Islam";
                var pdfBytes = await _ofrReportService.GeneratePdfReportAsync(filter, printUser);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error generating PreviewPdf in OFRReportController");
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Export Actions
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] OFRReportFilter filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                string printUser = User.Identity?.Name ?? "shariful Islam";
                var (fileBytes, contentType, fileName) = await _ofrReportService.ExportReportAsync(filter, exportFormat, printUser);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in Export in OFRReportController");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] OFRReportFilter filter)
        {
            return await Export(filter, "PDF");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] OFRReportFilter filter)
        {
            return await Export(filter, "Excel");
        }
        #endregion

        #region Private Helper: Grid Model Configuration
        private GridModel CreateGridModel(string ajaxUrl)
        {
            return new GridModel
            {
                GridId = "ofrReportTable",
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
                DefaultSortColumn = "ofrDate",
                DefaultSortDirection = "desc",
                Columns = new List<GridColumn>
                {
                    new() { Data = "slNo", Header = "Sl.No.", Width = "45px", Align = "center", Sortable = false },
                    new() { Data = "ofrDate", Header = "Date", Width = "85px", Align = "center", SortField = "Ofrdate" },
                    new() { Data = "ofrNo", Header = "OFR No", Width = "150px", Align = "left", SortField = "Ofrno" },
                    new() { Data = "jobNo", Header = "Job No", Width = "150px", Align = "left", SortField = "JobNo" },
                    new() { Data = "customerName", Header = "Customer Name", Width = "180px", Align = "left", SortField = "CustomerName" },
                    new() { Data = "shipmentMode", Header = "Mode", Width = "95px", Align = "center" },
                    new() { Data = "totalRequisitionAmount", Header = "Req. Amount", Width = "100px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "totalApprovedAmount", Header = "App. Amount", Width = "100px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "totalReceiptableAmount", Header = "Receiptable", Width = "95px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "totalNonReceiptableAmount", Header = "Non-Rece.", Width = "95px", Align = "right", RenderFn = "formatCurrency" },
                    new() { Data = "status", Header = "Status", Width = "85px", Align = "center" },
                    new() { Data = "action", Header = "Action", Width = "80px", Align = "center", Sortable = false }
                }
            };
        }
        #endregion
    }
}
