using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardIncomeStatement")]
    public class DashboardIncomeStatementController : BaseController
    {
        private readonly IDashboardIncomeStatementService _incomeStatementService;

        public DashboardIncomeStatementController(IDashboardIncomeStatementService incomeStatementService)
        {
            _incomeStatementService = incomeStatementService;
        }

        [HttpPost("GetData")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _incomeStatementService.GetIncomeStatementDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        [HttpGet("ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] DashboardFilterDTO filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new DashboardFilterDTO();
                var bytes = await _incomeStatementService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? "Start";
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? "End";
                var fileName = $"Income_Statement_{fromStr}_{toStr}.pdf";

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
        public async Task<IActionResult> ExportExcel([FromQuery] DashboardFilterDTO filter)
        {
            try
            {
                filter ??= new DashboardFilterDTO();
                var bytes = await _incomeStatementService.GenerateExcelAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? "Start";
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? "End";
                var fileName = $"Income_Statement_{fromStr}_{toStr}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export Excel: " + ex.Message);
            }
        }
    }
}

