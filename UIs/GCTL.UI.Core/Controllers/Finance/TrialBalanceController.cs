using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.TrialBalance;
using GCTL.Service.Finance.TrialBalance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class TrialBalanceController : BaseController
    {
        private readonly ITrialBalanceService _trialBalanceService;

        public TrialBalanceController(ITrialBalanceService trialBalanceService)
            : base()
        {
            _trialBalanceService = trialBalanceService;
        }

        [HttpGet]
        [Route("TrialBalance")]
        [Route("TrialBalance/Index")]
        public async Task<IActionResult> Index()
        {
            var model = await _trialBalanceService.GetInitialViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [Route("TrialBalance/GetReportData")]
        public async Task<IActionResult> GetReportData([FromBody] TrialBalanceFilterVM filter)
        {
            try
            {
                var report = await _trialBalanceService.GetTrialBalanceReportAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("TrialBalance/GetReportData")]
        public async Task<IActionResult> GetReportDataGet([FromQuery] TrialBalanceFilterVM filter)
        {
            try
            {
                var report = await _trialBalanceService.GetTrialBalanceReportAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("TrialBalance/GetBranches")]
        public async Task<IActionResult> GetBranches(string? companyCode)
        {
            var branches = await _trialBalanceService.GetBranchesAsync(companyCode);
            return Json(branches);
        }

        [HttpGet]
        [HttpPost]
        [Route("TrialBalance/ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] TrialBalanceFilterVM filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new TrialBalanceFilterVM();
                var pdfBytes = await _trialBalanceService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? "Start";
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? "End";
                var fileName = $"Trial_Balance_{fromStr}_{toStr}.pdf";

                if (!download)
                {
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(pdfBytes, "application/pdf");
                }

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export PDF: " + ex.Message);
            }
        }
    }
}
