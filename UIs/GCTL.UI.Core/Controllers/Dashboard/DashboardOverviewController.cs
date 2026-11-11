using System;
using System.Globalization;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardOverview")]
    [Route("Dashboard/Overview")]
    public class DashboardOverviewController : BaseController
    {
        private readonly IDashboardOverviewService _overviewService;
        private readonly IDashboardFilterService _filterService;

        public DashboardOverviewController(
            IDashboardOverviewService overviewService,
            IDashboardFilterService filterService)
        {
            _overviewService = overviewService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "overview";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _overviewService.GetOverviewDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        #region Report Export & Preview Actions

        [HttpGet("Export")]
        [HttpGet("ExportReport")]
        public async Task<IActionResult> Export([FromQuery] DashboardFilterDTO filter, [FromQuery] string exportFormat = "PDF", [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new DashboardFilterDTO();
                NormalizeDates(filter);

                string printUser = User?.Identity?.Name ?? "Administrator";
                string format = (exportFormat ?? "PDF").Trim().ToUpperInvariant();
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (format == "EXCEL" || format == "XLSX")
                {
                    var excelBytes = await _overviewService.GenerateExcelReportAsync(filter, printUser);
                    string fileName = $"Executive_Overview_Report_{timeStamp}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    // PDF or Preview
                    var pdfBytes = await _overviewService.GeneratePdfReportAsync(filter, printUser);
                    string fileName = $"Executive_Overview_Report_{timeStamp}.pdf";
                    if (download)
                    {
                        return File(pdfBytes, "application/pdf", fileName);
                    }
                    else
                    {
                        // Inline for browser iframe preview
                        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                        return File(pdfBytes, "application/pdf");
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Executive Overview report export failed: {ex.Message}");
            }
        }

        [HttpGet("ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] DashboardFilterDTO filter, [FromQuery] bool download = true)
        {
            return await Export(filter, "PDF", download);
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] DashboardFilterDTO filter)
        {
            return await Export(filter, "EXCEL", true);
        }

        private void NormalizeDates(DashboardFilterDTO filter)
        {
            if (filter.FromDate == null && !string.IsNullOrWhiteSpace(Request.Query["fromDate"]))
            {
                string raw = Request.Query["fromDate"].ToString().Trim();
                if (DateTime.TryParseExact(raw, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    filter.FromDate = dt;
                else if (DateTime.TryParse(raw, out var dt2))
                    filter.FromDate = dt2;
            }

            if (filter.ToDate == null && !string.IsNullOrWhiteSpace(Request.Query["toDate"]))
            {
                string raw = Request.Query["toDate"].ToString().Trim();
                if (DateTime.TryParseExact(raw, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    filter.ToDate = dt;
                else if (DateTime.TryParse(raw, out var dt2))
                    filter.ToDate = dt2;
            }
        }

        #endregion
    }
}
