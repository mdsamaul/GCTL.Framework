using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.CashFlow;
using GCTL.Service.Finance.CashFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class CashFlowController : BaseController
    {
        private readonly ICashFlowService _cashFlowService;

        public CashFlowController(ICashFlowService cashFlowService)
            : base()
        {
            _cashFlowService = cashFlowService;
        }

        [HttpGet]
        [Route("CashFlow")]
        [Route("CashFlow/Index")]
        public async Task<IActionResult> Index()
        {
            var model = await _cashFlowService.GetInitialViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [Route("CashFlow/GetReportData")]
        public async Task<IActionResult> GetReportData([FromBody] CashFlowFilterVM? filter)
        {
            try
            {
                filter ??= new CashFlowFilterVM();
                var report = await _cashFlowService.GetCashFlowReportAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("CashFlow/GetReportData")]
        public async Task<IActionResult> GetReportDataGet([FromQuery] CashFlowFilterVM? filter)
        {
            try
            {
                filter ??= new CashFlowFilterVM();
                var report = await _cashFlowService.GetCashFlowReportAsync(filter);
                return Json(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("CashFlow/GetBranches")]
        public async Task<IActionResult> GetBranches(string? companyCode)
        {
            var branches = await _cashFlowService.GetBranchesAsync(companyCode);
            return Json(branches);
        }

        [HttpGet]
        [HttpPost]
        [Route("CashFlow/ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] CashFlowFilterVM filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new CashFlowFilterVM();
                var pdfBytes = await _cashFlowService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? "Start";
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? "End";
                var fileName = $"Cash_Flow_{fromStr}_{toStr}.pdf";

                if (!download)
                {
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(pdfBytes, "application/pdf");
                }

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("CashFlow/ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] CashFlowFilterVM filter)
        {
            try
            {
                filter ??= new CashFlowFilterVM();
                var fileBytes = await _cashFlowService.ExportExcelAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? "Start";
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? "End";
                var fileName = $"Cash_Flow_{fromStr}_{toStr}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
