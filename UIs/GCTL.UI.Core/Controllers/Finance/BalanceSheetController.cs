using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.BalanceSheet;
using GCTL.Service.Finance.BalanceSheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class BalanceSheetController : BaseController
    {
        private readonly IBalanceSheetService _balanceSheetService;

        public BalanceSheetController(IBalanceSheetService balanceSheetService)
            : base()
        {
            _balanceSheetService = balanceSheetService;
        }

        [HttpGet]
        [Route("BalanceSheet")]
        [Route("BalanceSheet/Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Companies = await _balanceSheetService.GetCompaniesAsync();
            ViewBag.Branches = await _balanceSheetService.GetBranchesAsync();
            return View();
        }

        [HttpGet]
        [Route("BalanceSheet/GetReport")]
        public async Task<IActionResult> GetReport(BalanceSheetFilterVM filter)
        {
            try
            {
                var report = await _balanceSheetService.GetBalanceSheetAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("BalanceSheet/GetBranches")]
        public async Task<IActionResult> GetBranches(string? companyCode, string? companyId = null)
        {
            var code = !string.IsNullOrEmpty(companyCode) ? companyCode : companyId;
            var branches = await _balanceSheetService.GetBranchesAsync(code);
            return Json(branches);
        }

        [HttpGet]
        [HttpPost]
        [Route("BalanceSheet/ExportExcel")]
        public async Task<IActionResult> ExportExcel(BalanceSheetFilterVM filter)
        {
            try
            {
                filter ??= new BalanceSheetFilterVM();
                var fileBytes = await _balanceSheetService.ExportExcelAsync(filter);
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"BalanceSheet_{toStr}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("BalanceSheet/ExportPdf")]
        public async Task<IActionResult> ExportPdf(BalanceSheetFilterVM filter, bool preview = false, bool download = false)
        {
            try
            {
                filter ??= new BalanceSheetFilterVM();
                var fileBytes = await _balanceSheetService.GeneratePdfAsync(filter);
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"BalanceSheet_{toStr}.pdf";

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
