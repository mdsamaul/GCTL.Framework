using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.GeneralLedgerReport;
using GCTL.Service.Finance.GeneralLedgerReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class GeneralLedgerReportController : BaseController
    {
        private readonly IGeneralLedgerReportService _reportService;

        public GeneralLedgerReportController(IGeneralLedgerReportService reportService)
            : base()
        {
            _reportService = reportService;
        }

        [HttpGet]
        [Route("GeneralLedgerReport")]
        [Route("GeneralLedgerReport/Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Companies = await _reportService.GetCompaniesAsync();
            ViewBag.Branches = await _reportService.GetBranchesAsync();
            ViewBag.AccountHeads = await _reportService.GetAccountHeadsAsync();
            return View();
        }

        [HttpGet]
        [Route("GeneralLedgerReport/GetReport")]
        public async Task<IActionResult> GetReport(GeneralLedgerReportFilterVM filter)
        {
            try
            {
                var report = await _reportService.GetLedgerReportAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GeneralLedgerReport/GetBranches")]
        public async Task<IActionResult> GetBranches(string? companyCode, string? companyId = null)
        {
            var code = !string.IsNullOrEmpty(companyCode) ? companyCode : companyId;
            var branches = await _reportService.GetBranchesAsync(code);
            return Json(branches);
        }

        [HttpGet]
        [HttpPost]
        [Route("GeneralLedgerReport/ExportExcel")]
        public async Task<IActionResult> ExportExcel(GeneralLedgerReportFilterVM filter)
        {
            try
            {
                filter ??= new GeneralLedgerReportFilterVM();
                var fileBytes = await _reportService.ExportExcelAsync(filter);
                var fileName = $"GeneralLedger_{filter.GeneralLedgerCodeNo ?? "Report"}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("GeneralLedgerReport/ExportPdf")]
        public async Task<IActionResult> ExportPdf(GeneralLedgerReportFilterVM filter, bool preview = false, bool download = false)
        {
            try
            {
                filter ??= new GeneralLedgerReportFilterVM();
                var fileBytes = await _reportService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"GeneralLedger_{filter.GeneralLedgerCodeNo ?? "Report"}_{fromStr}_{toStr}.pdf";

                if (preview && !download)
                {
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(fileBytes, "application/pdf");
                }

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export PDF: " + ex.Message);
            }
        }
    }
}
