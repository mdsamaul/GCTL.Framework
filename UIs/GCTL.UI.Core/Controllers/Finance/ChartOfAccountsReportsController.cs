using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.ChartOfAccountsReports;
using GCTL.Service.Finance.ChartOfAccountsReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class ChartOfAccountsReportsController : BaseController
    {
        private readonly IChartOfAccountsReportsService _reportService;

        public ChartOfAccountsReportsController(IChartOfAccountsReportsService reportService)
            : base()
        {
            _reportService = reportService;
        }

        [HttpGet]
        [Route("ChartOfAccountsReports")]
        [Route("ChartOfAccountsReports/Index")]
        public async Task<IActionResult> Index()
        {
            var initialData = await _reportService.GetReportDataAsync(new ChartOfAccountsReportFilterVM());
            return View(initialData);
        }

        [HttpGet]
        [Route("ChartOfAccountsReports/GetData")]
        public async Task<IActionResult> GetData([FromQuery] ChartOfAccountsReportFilterVM filter)
        {
            try
            {
                var data = await _reportService.GetReportDataAsync(filter);
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("ChartOfAccountsReports/ExportExcel")]
        public async Task<IActionResult> ExportExcel(ChartOfAccountsReportFilterVM filter)
        {
            try
            {
                filter ??= new ChartOfAccountsReportFilterVM();
                var fileBytes = await _reportService.ExportExcelAsync(filter);
                var mode = string.Equals(filter.ViewMode, "list", StringComparison.OrdinalIgnoreCase) ? "List" : "Tree";
                var fileName = $"ChartOfAccounts_{mode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("ChartOfAccountsReports/ExportPdf")]
        public async Task<IActionResult> ExportPdf(ChartOfAccountsReportFilterVM filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new ChartOfAccountsReportFilterVM();
                var fileBytes = await _reportService.ExportPdfAsync(filter);
                var mode = string.Equals(filter.ViewMode, "list", StringComparison.OrdinalIgnoreCase) ? "List" : "Tree";
                var fileName = $"ChartOfAccounts_{mode}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                if (download || filter.Download)
                {
                    return File(fileBytes, "application/pdf", fileName);
                }

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                return File(fileBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export PDF: " + ex.Message);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("ChartOfAccountsReports/ExportCsv")]
        public async Task<IActionResult> ExportCsv(ChartOfAccountsReportFilterVM filter)
        {
            try
            {
                filter ??= new ChartOfAccountsReportFilterVM();
                var fileBytes = await _reportService.ExportCsvAsync(filter);
                var mode = string.Equals(filter.ViewMode, "list", StringComparison.OrdinalIgnoreCase) ? "List" : "Tree";
                var fileName = $"ChartOfAccounts_{mode}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export CSV: " + ex.Message);
            }
        }
    }
}
