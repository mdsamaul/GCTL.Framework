using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardTrialBalance")]
    public class DashboardTrialBalanceController : BaseController
    {
        private readonly IDashboardTrialBalanceService _trialBalanceService;

        public DashboardTrialBalanceController(IDashboardTrialBalanceService trialBalanceService)
        {
            _trialBalanceService = trialBalanceService;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _trialBalanceService.GetTrialBalanceDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        // Preview (download=false) / PDF download (download=true)
        [HttpGet("ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] DashboardFilterDTO filter, [FromQuery] int level = 4, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new DashboardFilterDTO();
                var bytes = await _trialBalanceService.GeneratePdfAsync(filter, level);
                var fileName = BuildFileName(filter, "pdf");

                if (!download)
                {
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(bytes, "application/pdf");
                }
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export PDF: " + ex.Message);
            }
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] DashboardFilterDTO filter, [FromQuery] int level = 4, [FromQuery] bool hideZero = true)
        {
            try
            {
                filter ??= new DashboardFilterDTO();
                var bytes = await _trialBalanceService.GenerateExcelAsync(filter, level, hideZero);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", BuildFileName(filter, "xlsx"));
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }

        private static string BuildFileName(DashboardFilterDTO f, string ext)
        {
            var from = f.FromDate?.ToString("yyyyMMdd") ?? "Start";
            var to = f.ToDate?.ToString("yyyyMMdd") ?? "End";
            return $"Trial_Balance_{from}_{to}.{ext}";
        }
    }
}
