using GCTL.Core.ViewModels.Finance.ChartOfAccountsReports;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccountsReports;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class ChartOfAccountsController : BaseController
    {
        private readonly IChartOfAccountsService service;
        private readonly IChartOfAccountsReportsService reportsService;

        public ChartOfAccountsController(
            IChartOfAccountsService service,
            IChartOfAccountsReportsService reportsService) : base()
        {
            this.service = service;
            this.reportsService = reportsService;
        }

        [HttpGet]
        [Route("ChartOfAccounts")]
        [Route("ChartOfAccounts/Index")]
        public async Task<IActionResult> Index()
        {
            var stats = await service.GetStatsAsync();
            ViewBag.Stats = stats;
            return View();
        }

        [HttpGet]
        [Route("ChartOfAccounts/ListView")]
        public async Task<IActionResult> ListView()
        {
            var stats = await service.GetStatsAsync();
            ViewBag.Stats = stats;
            return View();
        }

        [Route("ChartOfAccounts/GetListData")]
        [HttpGet]
        public async Task<IActionResult> GetListData([FromQuery] string search = "", [FromQuery] bool showArchived = false)
        {
            try
            {
                var filter = new ChartOfAccountsReportFilterVM
                {
                    SearchTerm = search,
                    ShowArchived = showArchived,
                    ViewMode = "list"
                };
                var reportData = await reportsService.GetReportDataAsync(filter);
                return Json(new { success = true, data = reportData?.List ?? new() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Route("ChartOfAccounts/GetTree")]
        [HttpGet]
        public async Task<IActionResult> GetTree(bool showArchived = false, string search = "")
        {
            var tree = await service.GetTreeAsync(showArchived, search);
            return Json(new { success = true, data = tree });
        }

        [Route("ChartOfAccounts/GetStats")]
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var stats = await service.GetStatsAsync();
            return Json(new { success = true, data = stats });
        }

        [Route("ChartOfAccounts/SeedDefault")]
        [HttpPost]
        public async Task<IActionResult> SeedDefault()
        {
            var result = await service.SeedDefaultChartOfAccountsAsync();
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("ChartOfAccounts/ExportCsv")]
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var bytes = await service.ExportCsvAsync();
            return File(bytes, "text/csv", $"ChartOfAccounts_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
    }
}
