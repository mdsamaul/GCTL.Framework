using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardBillEntry")]
    [Route("Dashboard/BillEntry")]
    public class DashboardBillEntryController : BaseController
    {
        private readonly IDashboardBillEntryService _billEntryService;
        private readonly IDashboardFilterService _filterService;

        public DashboardBillEntryController(
            IDashboardBillEntryService billEntryService,
            IDashboardFilterService filterService)
        {
            _billEntryService = billEntryService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "bills";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _billEntryService.GetBillEntryDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
    }
}
