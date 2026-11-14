using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardCashBank")]
    [Route("Dashboard/CashBank")]
    public class DashboardCashBankController : BaseController
    {
        private readonly IDashboardCashBankService _cashBankService;
        private readonly IDashboardFilterService _filterService;

        public DashboardCashBankController(
            IDashboardCashBankService cashBankService,
            IDashboardFilterService filterService)
        {
            _cashBankService = cashBankService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "accounts";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _cashBankService.GetCashBankSummaryAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
    }
}

