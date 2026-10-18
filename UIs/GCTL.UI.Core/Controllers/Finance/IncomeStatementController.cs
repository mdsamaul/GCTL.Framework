using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.IncomeStatement;
using GCTL.Service.Finance.IncomeStatement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class IncomeStatementController : BaseController
    {
        private readonly IIncomeStatementService _incomeStatementService;

        public IncomeStatementController(IIncomeStatementService incomeStatementService)
            : base()
        {
            _incomeStatementService = incomeStatementService;
        }

        [HttpGet]
        [Route("IncomeStatement")]
        [Route("IncomeStatement/Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Companies = await _incomeStatementService.GetCompaniesAsync();
            ViewBag.Branches = await _incomeStatementService.GetBranchesAsync();
            return View();
        }

        [HttpGet]
        [Route("IncomeStatement/GetReport")]
        public async Task<IActionResult> GetReport([FromQuery] IncomeStatementFilterVM? filter)
        {
            try
            {
                filter ??= new IncomeStatementFilterVM();
                var report = await _incomeStatementService.GetIncomeStatementAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("IncomeStatement/GetReport")]
        public async Task<IActionResult> GetReportPost([FromBody] IncomeStatementFilterVM? filter)
        {
            try
            {
                filter ??= new IncomeStatementFilterVM();
                var report = await _incomeStatementService.GetIncomeStatementAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("IncomeStatement/GetBranches")]
        public async Task<IActionResult> GetBranches(string? companyCode, string? companyId = null)
        {
            var code = !string.IsNullOrEmpty(companyCode) ? companyCode : companyId;
            var branches = await _incomeStatementService.GetBranchesAsync(code);
            return Json(branches);
        }

        [HttpGet]
        [HttpPost]
        [Route("IncomeStatement/ExportExcel")]
        public async Task<IActionResult> ExportExcel(IncomeStatementFilterVM filter)
        {
            try
            {
                filter ??= new IncomeStatementFilterVM();
                var fileBytes = await _incomeStatementService.ExportExcelAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"IncomeStatement_{fromStr}_{toStr}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("IncomeStatement/ExportPdf")]
        public async Task<IActionResult> ExportPdf(IncomeStatementFilterVM filter, bool preview = false, bool download = false)
        {
            try
            {
                filter ??= new IncomeStatementFilterVM();
                var fileBytes = await _incomeStatementService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"IncomeStatement_{fromStr}_{toStr}.pdf";

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
