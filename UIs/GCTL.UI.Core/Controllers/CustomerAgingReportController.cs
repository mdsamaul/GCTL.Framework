using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Service.CustomerAgingReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerAgingReportController : BaseController
    {
        #region Private Fields
        private readonly ICustomerAgingReportService _agingReportService;
        #endregion

        #region Constructor
        public CustomerAgingReportController(ICustomerAgingReportService agingReportService) : base()
        {
            _agingReportService = agingReportService;
        }
        #endregion

        #region Index Action
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var options = await _agingReportService.GetFilterOptionsAsync();
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.ReportTypes = new SelectList(options.ReportTypes);
                ViewBag.ExportFormats = new SelectList(options.ExportFormats);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing CustomerAgingReport Index");
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.ReportTypes = new SelectList(new List<string> { "Customer Aging", "Bill Wise Customer Aging" });
                ViewBag.ExportFormats = new SelectList(new List<string> { "Preview", "PDF", "Excel" });
            }

            var gridModel = CreateGridModel(Url.Action("GetGridData", "CustomerAgingReport")!);
            return View(gridModel);
        }
        #endregion

        #region Grid Data Action
        [HttpPost]
        public async Task<IActionResult> GetGridData(CustomerAgingFilter filter)
        {
            try
            {
                filter ??= new CustomerAgingFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    if (!string.IsNullOrEmpty(form["searchTerm"])) filter.SearchTerm = form["searchTerm"];
                    else if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

                    if (!string.IsNullOrEmpty(form["agingDate"])) filter.AgingDate = form["agingDate"];
                    if (!string.IsNullOrEmpty(form["customerId"])) filter.CustomerId = form["customerId"];
                    if (!string.IsNullOrEmpty(form["reportType"])) filter.ReportType = form["reportType"];

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

                    if (!string.IsNullOrEmpty(form["sortDirection"]))
                    {
                        filter.SortDirection = form["sortDirection"];
                    }
                    else if (!string.IsNullOrEmpty(sortDir))
                    {
                        filter.SortDirection = sortDir;
                    }
                }

                var result = await _agingReportService.GetGridDataAsync(filter);
                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.RecordsTotal,
                    recordsFiltered = result.RecordsFiltered,
                    data = result.Data ?? new List<CustomerAgingReportItemDTO>(),
                    summary = result.Summary
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching grid data in CustomerAgingReportController");
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<CustomerAgingReportItemDTO>(),
                    summary = new CustomerAgingSummaryDTO()
                });
            }
        }
        #endregion

        #region Preview & Export Actions
        [HttpGet]
        public async Task<IActionResult> PreviewPdf([FromQuery] CustomerAgingFilter filter)
        {
            try
            {
                string printUser = User.Identity?.Name ?? "shariful Islam";
                var pdfBytes = await _agingReportService.GeneratePdfReportAsync(filter, printUser);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error generating PreviewPdf in CustomerAgingReportController");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] CustomerAgingFilter filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                string printUser = User.Identity?.Name ?? "shariful Islam";
                var (fileBytes, contentType, fileName) = await _agingReportService.ExportReportAsync(filter, exportFormat, printUser);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in Export in CustomerAgingReportController");
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion

        #region Private Helper: Grid Model Configuration
        private GridModel CreateGridModel(string ajaxUrl)
        {
            return new GridModel
            {
                GridId = "customerAgingReportTable",
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
                ShowSearchBox = true,
                ShowRowNumbers = false,
                ShowSkeletonLoading = false,
                Striped = true,
                Columns = new List<GridColumn>
                {
                    new GridColumn { 
                        Data = "customerNo", 
                        Header = "<span class='user-select-none'>Sel.</span><br/><input type='checkbox' class='form-check-input m-0 shadow-none' id='chkSelectAll' title='Select All' />", 
                        Width = "38px", 
                        Align = "center", 
                        Sortable = false, 
                        RenderFn = "renderSelectCheckbox" 
                    },
                    new GridColumn { Data = "customerName", Header = "Customer Name", Width = "180px", Align = "left" },
                    new GridColumn { Data = "billNo", Header = "Bill No", Width = "110px", Align = "left", Sortable = false },
                    new GridColumn { Data = "ag_0_30", Header = "0-30", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_31_60", Header = "31-60", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_61_90", Header = "61-90", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_91_120", Header = "91-120", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_121_150", Header = "121-150", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_151_180", Header = "151-180", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_181_210", Header = "181-210", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_211_240", Header = "211-240", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_241_270", Header = "241-270", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_271_300", Header = "271-300", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_301_330", Header = "301-330", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_331_360", Header = "331-360", Width = "75px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "ag_Over360", Header = "> 360", Width = "80px", Align = "right", RenderFn = "formatCurrency" },
                    new GridColumn { Data = "total", Header = "Total", Width = "90px", Align = "right", RenderFn = "formatCurrency" }
                }
            };
        }
        #endregion
    }
}
